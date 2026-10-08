using Asp.Versioning;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Users;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Api.Controllers.Users;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class UserController : ControllerBase
{
    private readonly IMediator _mediator;

    public UserController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/user
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return this.ToCreatedResult(nameof(GetById), result.Data);
    }

    // PUT: api/v1/user/{id}
    [HttpPut("{id}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Update a user")]
    [EndpointDescription("Replaces the profile, role, and organization of a user.")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDTO>> Update(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { Id = id }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/user?organizationId={id}&role=OrgAdmin&role=Learner
    [HttpGet]
    [MapToApiVersion("1.0")]
    [EndpointSummary("List users")]
    [EndpointDescription("Returns users that have not been deleted, across all organizations unless organizationId is given. Repeat role to match any of several roles; omit it for every role. Ordered by last name then first name.")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<UserDTO>>> List(
        [FromQuery] Guid? organizationId,
        [FromQuery(Name = "role")] UserRole[]? roles,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUsersQuery(organizationId, roles ?? []), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/user/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get user by id")]
    [EndpointDescription("Get user details using id, Supported API version is 1.0.")]
    [MapToApiVersion("1.0")]
    [EnableRateLimiting("fixed")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserDTO>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }
}
