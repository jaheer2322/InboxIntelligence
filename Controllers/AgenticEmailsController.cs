
using Asp.Versioning;
using InboxIntelligence.Models.DTOs;
using InboxIntelligence.Services;
using Microsoft.AspNetCore.Mvc;

namespace InboxIntelligence.Controllers;

[ApiVersion("2.0")]
[ApiController]
[Route("api/v2/Emails")]
public sealed class AgenticEmailsController : ControllerBase
{
    private readonly IAgenticEmailService _agenticEmailService;

    public AgenticEmailsController(
        IAgenticEmailService agenticEmailService)
    {
        _agenticEmailService = agenticEmailService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<string>> Ask(
        [FromBody] AgenticEmailRequestDTO request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return BadRequest("Query is required.");
        }

        var answer = await _agenticEmailService.AskAsync(
            request.Query,
            cancellationToken);

        return Ok(new { answer });
    }
}
