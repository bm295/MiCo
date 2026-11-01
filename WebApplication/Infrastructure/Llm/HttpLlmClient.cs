using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MilkCoPOS.Application.Llm;

namespace MilkCoPOS.Infrastructure.Llm;

public sealed class HttpLlmClient(HttpClient http, IOptions<LlmOptions> options,
    ILogger<HttpLlmClient> logger) : ILlmClient
{
    public async Task<LlmResult> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!Uri.TryCreate(settings.Endpoint, UriKind.Absolute, out var endpoint) ||
            endpoint.Scheme != Uri.UriSchemeHttps || string.IsNullOrWhiteSpace(settings.Model) ||
            string.IsNullOrWhiteSpace(settings.ApiKey) || settings.TimeoutSeconds <= 0)
            throw new InvalidOperationException("Configure Llm HTTPS Endpoint, Model, ApiKey and positive TimeoutSeconds.");
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt);

        var requestId = Guid.NewGuid().ToString("N");
        var model = settings.Model;
        var timer = Stopwatch.StartNew();
        LlmUsage? usage = null;
        decimal? cost = null;
        var status = "error";
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(settings.TimeoutSeconds));
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
            request.Headers.Add("X-Request-Id", requestId);
            request.Content = JsonContent.Create(new
            {
                model, stream = false,
                messages = new[] { new { role = "user", content = prompt } }
            });
            using var response = await http.SendAsync(request, timeout.Token);
            response.EnsureSuccessStatusCode();
            using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token),
                cancellationToken: timeout.Token);
            var root = json.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw new JsonException("Expected response object.");
            if (root.TryGetProperty("model", out var returnedModel) && returnedModel.ValueKind == JsonValueKind.String)
                model = returnedModel.GetString()!;
            usage = ReadUsage(root);
            // Exact model match only: never apply an alias price to an unknown returned model.
            if (settings.Prices.TryGetValue(model, out var price)) cost = price.Estimate(usage);
            if (!root.TryGetProperty("choices", out var choices) || choices.ValueKind != JsonValueKind.Array ||
                choices.GetArrayLength() == 0 || choices[0].ValueKind != JsonValueKind.Object ||
                !choices[0].TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object ||
                !message.TryGetProperty("content", out var text) || text.ValueKind != JsonValueKind.String)
                throw new JsonException("Missing completion text.");
            var content = text.GetString()!;
            status = "success";
            return new LlmResult(content, new(requestId, model, timer.Elapsed.TotalMilliseconds, status, usage, cost));
        }
        catch (OperationCanceledException)
        {
            status = cancellationToken.IsCancellationRequested ? "cancelled" : "timeout";
            throw;
        }
        finally
        {
            timer.Stop();
            // Do not log prompts, completions, API keys, or exception messages.
            logger.LogInformation("LLM request {RequestId} model {Model} status {Status} latency_ms {LatencyMs} input_tokens {InputTokens} output_tokens {OutputTokens} cached_input_tokens {CachedInputTokens} total_tokens {TotalTokens} estimated_cost_usd {EstimatedCostUsd}",
                requestId, model, status, timer.Elapsed.TotalMilliseconds,
                usage?.InputTokens, usage?.OutputTokens, usage?.CachedInputTokens, usage?.TotalTokens, cost);
        }
    }

    public static LlmUsage? ReadUsage(JsonElement root)
    {
        if (!root.TryGetProperty("usage", out var u) || u.ValueKind != JsonValueKind.Object ||
            !u.TryGetProperty("prompt_tokens", out var input) || (input.ValueKind != JsonValueKind.Number || !input.TryGetInt64(out var i)) ||
            !u.TryGetProperty("completion_tokens", out var output) || (output.ValueKind != JsonValueKind.Number || !output.TryGetInt64(out var o)) ||
            i < 0 || o < 0 || i > long.MaxValue - o) return null;
        long cached = 0;
        if (u.TryGetProperty("prompt_tokens_details", out var details) && details.ValueKind == JsonValueKind.Object &&
            details.TryGetProperty("cached_tokens", out var c) && (c.ValueKind != JsonValueKind.Number || !c.TryGetInt64(out cached) || cached < 0 || cached > i))
            return null;
        return new(i, o, cached);
    }
}
