using Asp.Versioning;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Platform;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Controllers.Platform;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class PlatformController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlatformController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET: api/v1/platform/snapshot
    [HttpGet("snapshot")]
    [AllowAnonymous]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Public totals for the home page")]
    [EndpointDescription("Counts of organizations, learner accounts, and trainings. Returns totals only, with no names or records.")]
    [ProducesResponseType(typeof(PlatformSnapshotDTO), StatusCodes.Status200OK)]
    public async Task<ActionResult<PlatformSnapshotDTO>> Snapshot(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetPlatformSnapshotQuery(), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }
}
