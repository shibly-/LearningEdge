using Asp.Versioning;
using LearningEdge.Application.Models.Commands.Organizations;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Organizations;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Controllers.Organizations;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class OrganizationController : ControllerBase
{
    private readonly IMediator _mediator;

    public OrganizationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/organization
    [HttpPost]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<OrganizationDTO>> Create([FromBody] CreateOrganizationCommand command)
    {
        var organization = await _mediator.Send(command);
        return Ok(organization);
    }

    // GET: api/organization/{id}
    [HttpGet("{id}")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<OrganizationDTO>> GetById(Guid id)
    {
        var organization = await _mediator.Send(new GetOrganizationByIdQuery(id));
        if (organization == null)
        {
            return NotFound();
        }
        return Ok(organization);
    }
}
