using Microsoft.AspNetCore.Mvc;
using MilkCoPOS.Application.Evaluation;

namespace MilkCoPOS.Controllers;

[ApiController]
[Route("api/llm/evaluations")]
public sealed class RagEvaluationController : ControllerBase
{
    [HttpPost]
    public ActionResult<RagEvaluationResult> Evaluate(RagEvaluationRequest request)
    {
        try { return Ok(RagEvaluator.Evaluate(request)); }
        catch (ArgumentException error) { return BadRequest(new ProblemDetails
            { Status = 400, Title = "Invalid evaluation input", Detail = error.Message }); }
    }
}
