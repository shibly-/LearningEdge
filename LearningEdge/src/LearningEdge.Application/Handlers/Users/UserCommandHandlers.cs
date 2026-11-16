using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Users;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Users;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Handlers.Users;

public class UserCommandHandlers :
    IRequestHandler<CreateUserCommand, Result<Guid>>,
    IRequestHandler<UpdateUserCommand, Result<UserDTO>>,
    IRequestHandler<RegisterLearnerCommand, Result<Guid>>,
    IRequestHandler<ApproveUserCommand, Result<UserDTO>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;
    private readonly IPendingCredentialProtector? _credentialProtector;
    private readonly IKeycloakUserDirectory? _keycloakUsers;

    public UserCommandHandlers(
        IApplicationDbContext appDbContext,
        IMapper mapper,
        IPendingCredentialProtector? credentialProtector = null,
        IKeycloakUserDirectory? keycloakUsers = null)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
        _credentialProtector = credentialProtector;
        _keycloakUsers = keycloakUsers;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var organizationExists = await _appDbContext.Organizations
            .AnyAsync(organization => organization.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result<Guid>.NotFound($"No organization found with Id {request.OrganizationId}.");
        }

        var existingUser = await _appDbContext.Users
            .FirstOrDefaultAsync(
                user => user.Email == request.Email && user.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (existingUser is not null)
        {
            return Result<Guid>.Conflict(
                $"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        var user = new User(request.FirstName, request.LastName, request.Email, request.Role, request.OrganizationId);
        _appDbContext.Users.Add(user);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict(
                $"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        return Result<Guid>.Ok(user.Id);
    }

    public async Task<Result<UserDTO>> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _appDbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == request.Id, cancellationToken);

        if (user is null)
        {
            return Result<UserDTO>.NotFound($"No user found with Id {request.Id}.");
        }

        var organizationExists = await _appDbContext.Organizations
            .AnyAsync(organization => organization.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result<UserDTO>.NotFound($"No organization found with Id {request.OrganizationId}.");
        }

        var emailTaken = await _appDbContext.Users
            .AnyAsync(
                candidate => candidate.Email == request.Email
                    && candidate.OrganizationId == request.OrganizationId
                    && candidate.Id != request.Id,
                cancellationToken);

        if (emailTaken)
        {
            return Result<UserDTO>.Conflict(
                $"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        user.Update(request.FirstName, request.LastName, request.Email, request.Role, request.OrganizationId);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<UserDTO>.Conflict(
                $"User with email {request.Email} already exists in organization {request.OrganizationId}.");
        }

        return Result<UserDTO>.Ok(_mapper.Map<UserDTO>(user));
    }

    public async Task<Result<Guid>> Handle(RegisterLearnerCommand request, CancellationToken cancellationToken)
    {
        if (_credentialProtector is null)
        {
            return Result<Guid>.Fail("Registration is not configured.");
        }

        var email = request.Email.Trim();
        var organizationExists = await _appDbContext.Organizations
            .AnyAsync(organization => organization.Id == request.OrganizationId, cancellationToken);

        if (!organizationExists)
        {
            return Result<Guid>.NotFound($"No organization found with Id {request.OrganizationId}.");
        }

        var existingUser = await _appDbContext.Users
            .FirstOrDefaultAsync(
                user => user.Email == email && user.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (existingUser is not null)
        {
            var message = existingUser.ActivationStatus == UserActivationStatus.Pending
                ? $"A registration for {email} is already waiting for approval."
                : $"User with email {email} already exists in this organization.";
            return Result<Guid>.Conflict(message);
        }

        var registered = User.RegisterLearner(
            request.FirstName.Trim(),
            request.LastName?.Trim() ?? string.Empty,
            email,
            request.OrganizationId);
        registered.HoldPendingPassword(_credentialProtector.Protect(request.Password));
        _appDbContext.Users.Add(registered);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict($"User with email {email} already exists in this organization.");
        }

        return Result<Guid>.Ok(registered.Id);
    }

    public async Task<Result<UserDTO>> Handle(ApproveUserCommand request, CancellationToken cancellationToken)
    {
        if (_credentialProtector is null || _keycloakUsers is null)
        {
            return Result<UserDTO>.Fail("Approval is not configured.");
        }

        var user = await _appDbContext.Users
            .FirstOrDefaultAsync(candidate => candidate.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            return Result<UserDTO>.NotFound($"No user found with Id {request.UserId}.");
        }

        if (user.ActivationStatus != UserActivationStatus.Pending)
        {
            return Result<UserDTO>.Conflict("This user is already approved.");
        }

        var decision = await ApproverMayActivate(request, user.OrganizationId, cancellationToken);
        if (!decision.Success)
        {
            return Result<UserDTO>.Forbidden(decision.Error);
        }

        if (string.IsNullOrEmpty(user.PendingPasswordProtected))
        {
            return Result<UserDTO>.Fail(
                "This registration has no password to send to Keycloak. Ask the learner to register again.");
        }

        string password;
        try
        {
            password = _credentialProtector.Unprotect(user.PendingPasswordProtected);
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            return Result<UserDTO>.Fail(
                "The saved registration password could not be read. Ask the learner to register again.");
        }

        string keycloakUserId;
        try
        {
            keycloakUserId = await _keycloakUsers.CreateLearnerAsync(
                new KeycloakLearnerAccount(user.Email, user.FirstName, user.LastName, password),
                cancellationToken);
        }
        catch (KeycloakProvisioningException ex)
        {
            return Result<UserDTO>.Fail(ex.Message);
        }

        user.MarkApproved(keycloakUserId);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<UserDTO>.Conflict(
                $"User with email {user.Email} already exists in organization {user.OrganizationId}.");
        }

        return Result<UserDTO>.Ok(_mapper.Map<UserDTO>(user));
    }

    private async Task<Result<bool>> ApproverMayActivate(
        ApproveUserCommand request,
        Guid organizationId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ApproverEmail))
        {
            return request.ApproverHasPlatformRole
                ? Result<bool>.Ok(true)
                : Result<bool>.Forbidden("Only an organization admin or a super admin can approve a registration.");
        }

        var email = request.ApproverEmail.Trim();
        var matches = await _appDbContext.Users
            .AsNoTracking()
            .Where(candidate => candidate.Email == email)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (matches.Count > 1)
        {
            return Result<bool>.Forbidden(
                $"The email {email} belongs to more than one organization, so this sign-in cannot approve users.");
        }

        if (matches.Count == 0)
        {
            return request.ApproverHasPlatformRole
                ? Result<bool>.Ok(true)
                : Result<bool>.Forbidden("Only an organization admin or a super admin can approve a registration.");
        }

        var approver = matches[0];
        if (approver.ActivationStatus != UserActivationStatus.Active)
        {
            return Result<bool>.Forbidden("Your account is not active, so it cannot approve registrations.");
        }

        var allowed = approver.Role == UserRole.SysAdmin
            || (approver.Role == UserRole.OrgAdmin && approver.OrganizationId == organizationId);

        return allowed
            ? Result<bool>.Ok(true)
            : Result<bool>.Forbidden(
                "Only an organization admin of this organization or a super admin can approve a registration.");
    }
}
