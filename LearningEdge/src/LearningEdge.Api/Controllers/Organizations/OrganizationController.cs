using Asp.Versioning;
using LearningEdge.Api.Constants;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Models.Commands.Organizations;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Organizations;
using LearningEdge.Application.Models.Queries.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace LearningEdge.Api.Controllers.Organizations;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[ApiVersion("2.0")]
public class OrganizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/organization
    [HttpPost]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create(
        [FromBody] CreateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return this.ToCreatedResult(nameof(GetById), result.Data);
    }

    // PUT: api/v1/organization/{id}
    [HttpPut("{id}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Update an organization")]
    [EndpointDescription("Replaces the name and description of an organization.")]
    [ProducesResponseType(typeof(OrganizationDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<OrganizationDTO>> Update(
        Guid id,
        [FromBody] UpdateOrganizationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { Id = id }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/organization    
    [HttpGet]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get all organizations")]
    [EndpointDescription("Returns every organization that has not been deleted, ordered by name.")]
    [ProducesResponseType(typeof(IReadOnlyList<OrganizationDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<OrganizationDTO>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationsQuery(), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/organization/{id}/users
    [HttpGet("{id:guid}/users")]
    [MapToApiVersion("1.0")]    
    [EndpointSummary("Get the users of an organization")]
    [EndpointDescription("Returns every user in the organization that has not been deleted, ordered by last name then first name.")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [OutputCache(PolicyName = CachePolicies.DefaultQueryCache)]
    public async Task<ActionResult<IReadOnlyList<UserDTO>>> GetUsers(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUsersByOrganizationQuery(id), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/organization/{id}
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(OrganizationDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationDTO>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationByIdQuery(id), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/organization/{id}
    [HttpGet("{id}")]
    [MapToApiVersion("2.0")]
    [ProducesResponseType(typeof(OrganizationDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OrganizationDTO>> GetByIdExt(Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetOrganizationByIdQuery(id), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    [HttpGet]
    [MapToApiVersion("2.0")]
    public IActionResult GetV2() => Ok("v2");
}
