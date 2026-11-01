using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MilkCoPOS.Infrastructure.Llm;
using MilkCoPOS.Application.Llm;
using Xunit;

namespace WebApplication.Tests;

public class LlmMeasurementTests
{
    [Fact]
    public void PricesCachedTokensSeparately()
    {
        var price = new LlmPrice { InputPerMillion = 2, OutputPerMillion = 8, CachedInputPerMillion = 1 };
        Assert.Equal(0.0058m, price.Estimate(new(1000, 500, 200)));
        Assert.Null(price.Estimate(null));
        Assert.Null(new LlmPrice().Estimate(new(1000, 500, 0)));
        Assert.Null(new LlmPrice { InputPerMillion = 2, OutputPerMillion = 8 }.Estimate(new(1000, 500, 200)));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"usage\":null}")]
    [InlineData("{\"usage\":{\"prompt_tokens\":-1,\"completion_tokens\":1}}")]
    [InlineData("{\"usage\":{\"prompt_tokens\":\"10\",\"completion_tokens\":1}}")]
    [InlineData("{\"usage\":{\"prompt_tokens\":10,\"completion_tokens\":1,\"prompt_tokens_details\":{\"cached_tokens\":11}}}")]
    public void MissingOrInvalidUsageIsUnknown(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Null(HttpLlmClient.ReadUsage(document.RootElement));
    }

    [Fact]
    public async Task SuccessReturnsMeasuredUsageAndCostWithoutLoggingContent()
    {
        var log = new CaptureLogger();
        var client = Create(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("""{"model":"test","choices":[{"message":{"content":"private answer"}}],"usage":{"prompt_tokens":1000,"completion_tokens":500}}""", Encoding.UTF8, "application/json")
        })), log);
        var result = await client.CompleteAsync("private prompt");
        Assert.Equal("success", result.Measurement.Status);
        Assert.Equal(1500L, result.Measurement.Usage!.TotalTokens);
        Assert.Equal(0.006m, result.Measurement.EstimatedCostUsd);
        Assert.True(result.Measurement.LatencyMs >= 0);
        Assert.Single(log.Messages);
        Assert.DoesNotContain("private", log.Messages[0]);
        Assert.DoesNotContain("secret", log.Messages[0]);
    }

    [Fact]
    public async Task FailureIsLoggedAndPropagated()
    {
        var log = new CaptureLogger();
        var client = Create(new Handler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests))), log);
        await Assert.ThrowsAsync<HttpRequestException>(() => client.CompleteAsync("prompt"));
        Assert.Contains("status error", Assert.Single(log.Messages));
    }

    [Fact]
    public async Task CancellationIsLoggedAndPropagated()
    {
        var log = new CaptureLogger();
        var client = Create(new Handler(async (_, token) => { await Task.Delay(Timeout.Infinite, token); return new(); }), log);
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => client.CompleteAsync("prompt", cts.Token));
        Assert.Contains("status cancelled", Assert.Single(log.Messages));
    }

    private static HttpLlmClient Create(HttpMessageHandler handler, CaptureLogger log) => new(new HttpClient(handler),
        Options.Create(new LlmOptions { Endpoint = "https://example.invalid/chat/completions", ApiKey = "secret", Model = "test",
            Prices = new() { ["test"] = new() { InputPerMillion = 2, OutputPerMillion = 8 } } }), log);

    private sealed class Handler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) => send(request, cancellationToken);
    }
    private sealed class CaptureLogger : ILogger<HttpLlmClient>
    {
        public List<string> Messages { get; } = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
