using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Platform;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Handlers.Platform;

public class PlatformQueryHandlers : IRequestHandler<GetPlatformSnapshotQuery, Result<PlatformSnapshotDTO>>
{
    private readonly IApplicationDbContext _context;

    public PlatformQueryHandlers(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Result<PlatformSnapshotDTO>> Handle(
        GetPlatformSnapshotQuery request,
        CancellationToken cancellationToken)
    {
        var organizationsActive = await _context.Organizations
            .CountAsync(organization => organization.IsActive, cancellationToken);
        var organizationsInactive = await _context.Organizations
            .CountAsync(organization => !organization.IsActive, cancellationToken);

        var learners = _context.Users.Where(user => user.Role == UserRole.Learner);
        var learnersActive = await learners
            .CountAsync(user => user.ActivationStatus == UserActivationStatus.Active, cancellationToken);
        var learnersPending = await learners
            .CountAsync(user => user.ActivationStatus == UserActivationStatus.Pending, cancellationToken);
        var learnersInactive = await learners
            .CountAsync(user => user.ActivationStatus == UserActivationStatus.Inactive, cancellationToken);

        var trainingsActive = await _context.Trainings
            .CountAsync(training => training.IsActive, cancellationToken);
        var trainingsInactive = await _context.Trainings
            .CountAsync(training => !training.IsActive, cancellationToken);

        return Result<PlatformSnapshotDTO>.Ok(new PlatformSnapshotDTO(
            organizationsActive,
            organizationsInactive,
            learnersActive,
            learnersPending,
            learnersInactive,
            trainingsActive,
            trainingsInactive));
    }
}
