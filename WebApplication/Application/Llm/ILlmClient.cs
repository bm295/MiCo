namespace MilkCoPOS.Application.Llm;

public record LlmUsage(long InputTokens, long OutputTokens, long CachedInputTokens)
{
    public long TotalTokens => InputTokens + OutputTokens;
}

public record LlmMeasurement(string RequestId, string Model, double LatencyMs,
    string Status, LlmUsage? Usage, decimal? EstimatedCostUsd);
public record LlmResult(string Text, LlmMeasurement Measurement);

public interface ILlmClient
{
    Task<LlmResult> CompleteAsync(string prompt, CancellationToken cancellationToken = default);
}
