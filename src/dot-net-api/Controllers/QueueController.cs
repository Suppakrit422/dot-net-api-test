using dot_net_api.Models;
using dot_net_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace dot_net_api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QueueController(IQueueService queueService) : ControllerBase
{
    /// <summary>Issue the next queue number. POST /queue</summary>
    [HttpPost]
    [ProducesResponseType<QueueTicketResponse>(StatusCodes.Status201Created)]
    public async Task<ActionResult<QueueTicketResponse>> Issue(CancellationToken ct)
    {
        var ticket = await queueService.IssueTicketAsync(ct);
        // 201 without a Location header: there is no GET-by-id endpoint to point to.
        return StatusCode(StatusCodes.Status201Created, ticket);
    }

    /// <summary>Start a new cycle so the next ticket is A0. POST /api/queue/reset</summary>
    /// <response code="200">The new cycle.</response>
    /// <response code="409">The current cycle has no tickets yet, so there is nothing to reset.</response>
    [HttpPost("reset")]
    [ProducesResponseType<QueueCycleResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QueueCycleResponse>> Reset(CancellationToken ct)
    {
        var cycle = await queueService.ResetAsync(ct);
        return cycle is null
            ? Problem(
                title: "Nothing to reset",
                detail: "The current cycle has no tickets yet.",
                statusCode: StatusCodes.Status409Conflict)
            : Ok(cycle);
    }

    /// <summary>The latest ticket of the current cycle. GET /queue/current</summary>
    [HttpGet("current")]
    [ProducesResponseType<QueueTicketResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QueueTicketResponse>> GetCurrent(CancellationToken ct)
    {
        var ticket = await queueService.GetCurrentAsync(ct);
        return ticket is null ? NotFound() : Ok(ticket);
    }
}
