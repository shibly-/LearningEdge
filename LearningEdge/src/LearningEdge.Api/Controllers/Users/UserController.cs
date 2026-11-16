using Asp.Versioning;
using LearningEdge.Api.Constants;
using LearningEdge.Api.Extensions;
using LearningEdge.Api.Security;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Users;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using System.Security.Claims;
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

    // POST: api/v1/user/organization-admin
    [HttpPost("organization-admin")]
    [Authorize(Policy = AuthPolicies.SysAdmin)]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Create an organization admin")]
    [EndpointDescription("System admin only. Creates the organization admin in LearningEdge and in Keycloak, so the person can sign in with the password in this request.")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> CreateOrganizationAdmin(
        [FromBody] CreateOrganizationAdminCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return this.ToCreatedResult(nameof(GetById), result.Data);
    }

    // POST: api/v1/user/register
    [HttpPost("register")]
    [AllowAnonymous]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Register a trainee")]
    [EndpointDescription("Creates a Learner that stays pending until an OrgAdmin or SysAdmin approves it. Does not create a Keycloak user.")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Register(
        [FromBody] RegisterLearnerCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return this.ToCreatedResult(nameof(GetById), result.Data);
    }

    // POST: api/v1/user/{id}/approve
    [HttpPost("{id:guid}/approve")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Approve a pending trainee")]
    [EndpointDescription("OrgAdmin (same organization) or SysAdmin only. Creates the user in Keycloak and activates the account.")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDTO>> Approve(Guid id, CancellationToken cancellationToken)
    {
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email")
            ?? User.FindFirstValue("preferred_username")
            ?? string.Empty;
        var hasPlatformRole = KeycloakRoles.Has(User, "OrgAdmin");

        if (string.IsNullOrWhiteSpace(email) && !hasPlatformRole)
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(
            new ApproveUserCommand(id, email, hasPlatformRole),
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
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
    [Authorize(Policy = AuthPolicies.SysAdmin)]
    [MapToApiVersion("1.0")]
    [EndpointSummary("List users")]
    [EndpointDescription("Returns users that have not been deleted, across all organizations unless organizationId is given. Repeat role to match any of several roles; omit it for every role. Ordered by last name then first name.")]
    [ProducesResponseType(typeof(IReadOnlyList<UserDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]    
    public async Task<ActionResult<IReadOnlyList<UserDTO>>> List(
        [FromQuery] Guid? organizationId,
        [FromQuery(Name = "role")] UserRole[]? roles,
        [FromQuery] UserActivationStatus? activationStatus,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetUsersQuery(organizationId, roles ?? [], activationStatus),
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/user/me
    // Declared before the {id} route for readability only: ASP.NET Core always
    // prefers a literal segment over a parameter, whatever the order here.
    [HttpGet("me")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get the signed-in user")]
    [EndpointDescription("Returns the user record matching the bearer token's principal, so the web app can resolve its own identity, role and organization.")]
    [ProducesResponseType(typeof(UserDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserDTO>> GetMe(CancellationToken cancellationToken)
    {
        // JwtBearer maps the inbound "email" claim onto ClaimTypes.Email by
        // default; the raw names are the fallback if MapInboundClaims is disabled.
        var email = User.FindFirstValue(ClaimTypes.Email)
            ?? User.FindFirstValue("email")
            ?? User.FindFirstValue("preferred_username");

        if (string.IsNullOrWhiteSpace(email))
        {
            return Unauthorized();
        }

        var result = await _mediator.Send(new GetUserByEmailQuery(email), cancellationToken);
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
