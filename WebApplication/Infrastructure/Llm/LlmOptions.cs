using MilkCoPOS.Application.Llm;

namespace MilkCoPOS.Infrastructure.Llm;

public sealed class LlmOptions
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "";
    public int TimeoutSeconds { get; set; } = 60;
    public Dictionary<string, LlmPrice> Prices { get; set; } = new();
}

// USD per one million tokens. Null means unknown, zero explicitly means free.
public sealed class LlmPrice
{
    public decimal? InputPerMillion { get; set; }
    public decimal? OutputPerMillion { get; set; }
    public decimal? CachedInputPerMillion { get; set; }

    public decimal? Estimate(LlmUsage? usage)
    {
        if (usage is null || InputPerMillion is null || OutputPerMillion is null ||
            InputPerMillion < 0 || OutputPerMillion < 0 ||
            (usage.CachedInputTokens > 0 && (CachedInputPerMillion is null || CachedInputPerMillion < 0)))
            return null;
        return ((usage.InputTokens - usage.CachedInputTokens) * InputPerMillion.Value +
            usage.OutputTokens * OutputPerMillion.Value +
            usage.CachedInputTokens * (CachedInputPerMillion ?? 0)) / 1_000_000m;
    }
}
