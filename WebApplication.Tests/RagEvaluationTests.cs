using MilkCoPOS.Application.Evaluation;
using MilkCoPOS.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace WebApplication.Tests;

public class RagEvaluationTests
{
    private static RagEvaluationRequest Sample() => new("order-1", "What is validated?",
        "Stock is validated. Payment is automatic.", 3,
        new() { ["stock"] = 3, ["other"] = 1 },
        [new("noise", "Unrelated."), new("stock", "Stock is validated.")],
        [new("Stock is validated.", "supported", [new("stock", "Stock is validated.")]),
         new("Payment is automatic.", "unsupported", [])], "reviewer-1");

    [Fact]
    public void ComputesRankSensitiveMetricsAndPenalizesMissingSlots()
    {
        var result = RagEvaluator.Evaluate(Sample());
        Assert.Equal(1.0 / 3, result.Retrieval.PrecisionAtK, 10);
        Assert.Equal(0.5, result.Retrieval.RecallAtK!.Value, 10);
        Assert.Equal(0.5, result.Retrieval.ReciprocalRankAtK!.Value, 10);
        Assert.Equal((7 / Math.Log2(3)) / (7 + 1 / Math.Log2(3)), result.Retrieval.NdcgAtK!.Value, 10);
        Assert.Equal(0.5, result.Faithfulness.Score!.Value);
        Assert.Equal(1, result.Faithfulness.ReviewCoverage);
    }

    [Fact]
    public void EmptyResultsHaveZeroScoresButUnknownFaithfulness()
    {
        var result = RagEvaluator.Evaluate(Sample() with { Retrieved = [], Claims = [] });
        Assert.Equal(0, result.Retrieval.PrecisionAtK);
        Assert.Equal(0, result.Retrieval.RecallAtK);
        Assert.Equal(0, result.Retrieval.NdcgAtK);
        Assert.Null(result.Faithfulness.Score);
        Assert.Equal(0, result.Faithfulness.ReviewCoverage);
    }

    [Fact]
    public void NoRelevantGoldIsUndefinedRatherThanPerfect()
    {
        var result = RagEvaluator.Evaluate(Sample() with { Relevance = new() });
        Assert.Null(result.Retrieval.RecallAtK);
        Assert.Null(result.Retrieval.ReciprocalRankAtK);
        Assert.Null(result.Retrieval.NdcgAtK);
    }

    [Fact]
    public void UnreviewedClaimsReduceCoverageAndContradictionsReduceScore()
    {
        var request = Sample() with { Claims = [
            new("Stock is validated.", "contradicted", [new("stock", "Stock is validated.")]),
            new("Payment is automatic.", "unreviewed", [])] };
        var result = RagEvaluator.Evaluate(request);
        Assert.Equal(0, result.Faithfulness.Score);
        Assert.Equal(0.5, result.Faithfulness.ReviewCoverage);
        Assert.Equal(1, result.Faithfulness.Contradicted);
    }

    [Fact]
    public void RejectsDuplicateResultsInvalidGradesAndInvalidK()
    {
        Assert.Throws<ArgumentException>(() => RagEvaluator.Evaluate(Sample() with { K = 0 }));
        Assert.Throws<ArgumentException>(() => RagEvaluator.Evaluate(Sample() with { Relevance = new() { ["stock"] = -1 } }));
        Assert.Throws<ArgumentException>(() => RagEvaluator.Evaluate(Sample() with { Retrieved = [new("x", "a"), new("x", "b")] }));
    }

    [Theory]
    [InlineData("missing", "Stock is validated.", 3)]
    [InlineData("stock", "fabricated quote", 3)]
    [InlineData("stock", "Stock is validated.", 1)]
    public void RejectsMissingFabricatedOrOutOfContextEvidence(string id, string quote, int k)
    {
        var request = Sample() with { K = k, Claims = [new("Stock is validated.", "supported", [new(id, quote)])] };
        Assert.Throws<ArgumentException>(() => RagEvaluator.Evaluate(request));
    }

    [Fact]
    public void ControllerReturnsBadRequestForInvalidInput()
    {
        Assert.IsType<BadRequestObjectResult>(new RagEvaluationController().Evaluate(Sample() with { K = 0 }).Result);
        Assert.IsType<OkObjectResult>(new RagEvaluationController().Evaluate(Sample()).Result);
    }
}
