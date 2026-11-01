namespace MilkCoPOS.Application.Evaluation;

public sealed record RetrievedChunk(string Id, string Text);
public sealed record Evidence(string ChunkId, string Quote);
// Verdicts are supplied by an independent human reviewer, not inferred from citations.
public sealed record ClaimReview(string Text, string Verdict, Evidence[] Evidence);
public sealed record RagEvaluationRequest(string CaseId, string Question, string Answer,
    int K, Dictionary<string, int> Relevance, RetrievedChunk[] Retrieved,
    ClaimReview[] Claims, string Reviewer);
public sealed record RetrievalMetrics(double PrecisionAtK, double? RecallAtK,
    double? ReciprocalRankAtK, double? NdcgAtK);
public sealed record FaithfulnessMetrics(double? Score, double ReviewCoverage,
    int Supported, int Unsupported, int Contradicted, int Unreviewed);
public sealed record RagEvaluationResult(string CaseId, RetrievalMetrics Retrieval,
    FaithfulnessMetrics Faithfulness, string Method = "human-annotated-v1");

public static class RagEvaluator
{
    public static RagEvaluationResult Evaluate(RagEvaluationRequest input)
    {
        ArgumentNullException.ThrowIfNull(input);
        void Require(bool valid, string message)
        {
            if (!valid) throw new ArgumentException(message);
        }
        Require(!string.IsNullOrWhiteSpace(input.CaseId) && !string.IsNullOrWhiteSpace(input.Question)
            && !string.IsNullOrWhiteSpace(input.Answer) && !string.IsNullOrWhiteSpace(input.Reviewer),
            "CaseId, Question, Answer and Reviewer are required.");
        Require(input.K is >= 1 and <= 100, "K must be between 1 and 100.");
        Require(input.Relevance is not null && input.Retrieved is not null && input.Claims is not null,
            "Relevance, Retrieved and Claims are required.");
        Require(input.Relevance!.All(x => !string.IsNullOrWhiteSpace(x.Key) && x.Value is >= 0 and <= 3),
            "Relevance grades must be 0..3 with nonempty chunk IDs.");
        Require(input.Retrieved!.All(x => x is not null && !string.IsNullOrWhiteSpace(x.Id)
            && !string.IsNullOrWhiteSpace(x.Text)), "Retrieved chunks require IDs and text.");
        Require(input.Retrieved.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() == input.Retrieved.Length,
            "Duplicate retrieved chunk IDs are not allowed.");
        var top = input.Retrieved.Take(input.K).ToArray();
        var context = top.ToDictionary(x => x.Id, x => x.Text, StringComparer.Ordinal);
        foreach (var claim in input.Claims!)
        {
            Require(claim is not null && !string.IsNullOrWhiteSpace(claim.Text)
                && input.Answer.Contains(claim.Text, StringComparison.Ordinal),
                "Each claim must be an exact nonempty span of the answer.");
            Require(claim!.Verdict is "supported" or "unsupported" or "contradicted" or "unreviewed",
                "Unknown claim verdict.");
            Require(claim.Evidence is not null, "Claim evidence array is required.");
            Require(claim.Verdict is not ("supported" or "contradicted") || claim.Evidence!.Length > 0,
                "Supported and contradicted claims require evidence.");
            foreach (var evidence in claim.Evidence!)
                Require(evidence is not null && !string.IsNullOrWhiteSpace(evidence.ChunkId)
                    && !string.IsNullOrWhiteSpace(evidence.Quote)
                    && context.TryGetValue(evidence.ChunkId, out var source)
                    && source.Contains(evidence.Quote, StringComparison.Ordinal),
                    "Evidence must quote a retrieved top-K chunk exactly.");
        }
        Require(input.Claims.Select(x => x.Text).Distinct(StringComparer.Ordinal).Count() == input.Claims.Length,
            "Duplicate claims are not allowed.");
        var grades = top.Select(x => input.Relevance.GetValueOrDefault(x.Id)).ToArray();
        var hits = grades.Count(x => x > 0);
        var relevant = input.Relevance.Count(x => x.Value > 0);
        var first = Array.FindIndex(grades, x => x > 0);
        double Dcg(IEnumerable<int> values) => values.Select((grade, rank) =>
            (Math.Pow(2, grade) - 1) / Math.Log2(rank + 2)).Sum();
        var ideal = Dcg(input.Relevance.Values.OrderDescending().Take(input.K));
        var supported = input.Claims.Count(x => x.Verdict == "supported");
        var unsupported = input.Claims.Count(x => x.Verdict == "unsupported");
        var contradicted = input.Claims.Count(x => x.Verdict == "contradicted");
        var unreviewed = input.Claims.Count(x => x.Verdict == "unreviewed");
        var reviewed = supported + unsupported + contradicted;
        return new(input.CaseId,
            new((double)hits / input.K, relevant == 0 ? null : (double)hits / relevant,
                relevant == 0 ? null : first < 0 ? 0 : 1.0 / (first + 1),
                ideal == 0 ? null : Dcg(grades) / ideal),
            new(reviewed == 0 ? null : (double)supported / reviewed,
                input.Claims.Length == 0 ? 0 : (double)reviewed / input.Claims.Length,
                supported, unsupported, contradicted, unreviewed));
    }
}
