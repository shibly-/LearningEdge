using Asp.Versioning;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Users;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;


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

    // POST: api/user
    [HttpPost]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<UserDTO>> Create([FromBody] CreateUserCommand command)
    {
        var user = await _mediator.Send(command);
        return Ok(user);
    }

    // GET: api/user/{id}
    [HttpGet("{id}")]
    [EndpointSummary("Get user by id")]
    [EndpointDescription("Get user details using id, Supported API version is 1.0.")]
    [MapToApiVersion("1.0")]
    public async Task<ActionResult<UserDTO>> GetById(Guid id)
    {
        var user = await _mediator.Send(new GetUserByIdQuery(id));
        if (user == null)
        {
            return NotFound();
        }
        return Ok(user);
    }
}
