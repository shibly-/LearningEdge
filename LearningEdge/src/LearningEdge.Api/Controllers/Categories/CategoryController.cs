using Asp.Versioning;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Models.Commands.Categories;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Categories;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Controllers.Categories;

[ApiController]
[Route("api/v{version:apiVersion}/organization/{organizationId:guid}/category")]
[ApiVersion("1.0")]
public class CategoryController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoryController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/organization/{organizationId}/category
    [HttpPost]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Create a category in an organization")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create(
        Guid organizationId,
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { OrganizationId = organizationId }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        RouteData.Values.TryGetValue("version", out var version);
        return CreatedAtAction(nameof(GetById), new { version, organizationId, id = result.Data }, result.Data);
    }

    // PUT: api/v1/organization/{organizationId}/category/{id}
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Update a category")]
    [EndpointDescription("Replaces the name, description and active flag of a category in the organization.")]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CategoryDTO>> Update(
        Guid organizationId,
        Guid id,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { OrganizationId = organizationId, Id = id }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/organization/{organizationId}/category
    [HttpGet]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get the categories of an organization")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<CategoryDTO>>> GetAll(Guid organizationId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoriesQuery(organizationId), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/organization/{organizationId}/category/{id}
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(CategoryDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDTO>> GetById(Guid organizationId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(organizationId, id), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }
}
