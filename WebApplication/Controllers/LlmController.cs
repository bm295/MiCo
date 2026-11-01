using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MilkCoPOS.Application.Llm;

namespace MilkCoPOS.Controllers;

[ApiController]
[Route("api/llm")]
public sealed class LlmController(ILlmClient client) : ControllerBase
{
    [HttpPost("complete")]
    public async Task<ActionResult<LlmResult>> Complete(CompletionRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await client.CompleteAsync(request.Prompt, cancellationToken)); }
        catch (InvalidOperationException) { return Problem(statusCode: 503, detail: "LLM is not configured."); }
        catch (HttpRequestException) { return Problem(statusCode: 502, detail: "LLM provider request failed."); }
        catch (System.Text.Json.JsonException) { return Problem(statusCode: 502, detail: "Invalid LLM provider response."); }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        { return Problem(statusCode: 504, detail: "LLM provider timed out."); }
    }
}

public sealed record CompletionRequest([property: Required, StringLength(16000, MinimumLength = 1)] string Prompt);
