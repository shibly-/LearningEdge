using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Files;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Trainings;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Trainings;
using MediatR;
using Microsoft.EntityFrameworkCore;
using static LearningEdge.Domain.Common.Enums;

namespace LearningEdge.Application.Handlers.Trainings;

public class TrainingCommandHandlers :
    IRequestHandler<CreateTrainingCommand, Result<Guid>>,
    IRequestHandler<UpdateTrainingCommand, Result<TrainingDTO>>,
    IRequestHandler<UploadTrainingFilesCommand, Result<IReadOnlyList<TrainingFileDTO>>>,
    IRequestHandler<RemoveTrainingFileCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;
    private readonly IFileStorage _fileStorage;

    public TrainingCommandHandlers(IApplicationDbContext appDbContext, IMapper mapper, IFileStorage fileStorage)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
        _fileStorage = fileStorage;
    }

    public async Task<Result<Guid>> Handle(CreateTrainingCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await _appDbContext.Categories
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return Result<Guid>.NotFound($"No category found with Id {request.CategoryId}.");
        }

        var nameTaken = await _appDbContext.Trainings
            .AnyAsync(
                training => training.CategoryId == request.CategoryId && training.Name == request.Name,
                cancellationToken);

        if (nameTaken)
        {
            return Result<Guid>.Conflict(NameTakenMessage(request.Name));
        }

        var training = new Training(request.CategoryId, request.Name, request.Description ?? string.Empty, request.IsActive);
        _appDbContext.Trainings.Add(training);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict(NameTakenMessage(request.Name));
        }

        return Result<Guid>.Ok(training.Id);
    }

    public async Task<Result<TrainingDTO>> Handle(UpdateTrainingCommand request, CancellationToken cancellationToken)
    {
        var training = await _appDbContext.Trainings
            .Include(candidate => candidate.Files)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CategoryId == request.CategoryId,
                cancellationToken);

        if (training is null)
        {
            return Result<TrainingDTO>.NotFound($"No training found with Id {request.Id} in category {request.CategoryId}.");
        }

        var nameTaken = await _appDbContext.Trainings
            .AnyAsync(
                candidate => candidate.CategoryId == request.CategoryId
                    && candidate.Name == request.Name
                    && candidate.Id != request.Id,
                cancellationToken);

        if (nameTaken)
        {
            return Result<TrainingDTO>.Conflict(NameTakenMessage(request.Name));
        }

        training.Update(request.Name, request.Description ?? string.Empty, request.IsActive);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<TrainingDTO>.Conflict(NameTakenMessage(request.Name));
        }

        return Result<TrainingDTO>.Ok(_mapper.Map<TrainingDTO>(training));
    }

    public async Task<Result<IReadOnlyList<TrainingFileDTO>>> Handle(
        UploadTrainingFilesCommand request,
        CancellationToken cancellationToken)
    {
        var target = await (
                from training in _appDbContext.Trainings
                join category in _appDbContext.Categories on training.CategoryId equals category.Id
                where training.Id == request.TrainingId && training.CategoryId == request.CategoryId
                select new { Training = training, category.OrganizationId })
            .FirstOrDefaultAsync(cancellationToken);

        if (target is null)
        {
            return Result<IReadOnlyList<TrainingFileDTO>>.NotFound(
                $"No training found with Id {request.TrainingId} in category {request.CategoryId}.");
        }

        var uploader = await _appDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == request.UploadedByUserId, cancellationToken);

        var isAdmin = uploader is not null
            && (uploader.Role == UserRole.SysAdmin
                || (uploader.Role == UserRole.OrgAdmin && uploader.OrganizationId == target.OrganizationId));

        if (!isAdmin)
        {
            return Result<IReadOnlyList<TrainingFileDTO>>.Forbidden(
                "Only an organization admin or system admin can upload training files.");
        }

        foreach (var file in request.Files)
        {
            if (!await TrainingFileRules.HasExpectedContentAsync(file.Content, file.FileName, cancellationToken))
            {
                return Result<IReadOnlyList<TrainingFileDTO>>.Validation(
                    $"File '{file.FileName}' does not match its extension.");
            }
        }

        var storedKeys = new List<string>();
        var added = new List<TrainingFile>();

        try
        {
            foreach (var file in request.Files)
            {
                var displayName = Path.GetFileName(file.FileName);
                var extension = Path.GetExtension(displayName).ToLowerInvariant();
                var key = await _fileStorage.SaveAsync(
                    file.Content, $"trainings/{target.Training.Id}", extension, cancellationToken);
                storedKeys.Add(key);

                var entity = target.Training.AddFile(
                    displayName,
                    TrainingFileRules.ContentTypeFor(displayName),
                    file.Length,
                    key,
                    request.UploadedByUserId);
                _appDbContext.TrainingFiles.Add(entity);
                added.Add(entity);
            }

            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            foreach (var key in storedKeys)
            {
                await _fileStorage.DeleteAsync(key, CancellationToken.None);
            }

            throw;
        }

        return Result<IReadOnlyList<TrainingFileDTO>>.Ok(_mapper.Map<List<TrainingFileDTO>>(added));
    }

    public async Task<Result<Guid>> Handle(RemoveTrainingFileCommand request, CancellationToken cancellationToken)
    {
        // Include is ignored inside a projection, so resolve the organization first, then load files.
        var organizationId = await (
                from training in _appDbContext.Trainings
                join category in _appDbContext.Categories on training.CategoryId equals category.Id
                where training.Id == request.TrainingId && training.CategoryId == request.CategoryId
                select (Guid?)category.OrganizationId)
            .FirstOrDefaultAsync(cancellationToken);

        if (organizationId is null)
        {
            return Result<Guid>.NotFound(
                $"No training found with Id {request.TrainingId} in category {request.CategoryId}.");
        }

        var actor = await _appDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == request.RemovedByUserId, cancellationToken);

        var isAdmin = actor is not null
            && (actor.Role == UserRole.SysAdmin
                || (actor.Role == UserRole.OrgAdmin && actor.OrganizationId == organizationId));

        if (!isAdmin)
        {
            return Result<Guid>.Forbidden(
                "Only an organization admin or system admin can remove training files.");
        }

        var trainingEntity = await _appDbContext.Trainings
            .Include(candidate => candidate.Files)
            .FirstAsync(candidate => candidate.Id == request.TrainingId, cancellationToken);

        var file = trainingEntity.RemoveFile(request.FileId);
        if (file is null)
        {
            return Result<Guid>.NotFound(
                $"No file found with Id {request.FileId} on training {request.TrainingId}.");
        }

        // Drop the bytes first. Delete is idempotent, so a failed save can be retried.
        await _fileStorage.DeleteAsync(file.StorageKey, cancellationToken);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Ok(file.Id);
    }

    private static string NameTakenMessage(string name) =>
        $"A training named {name} already exists in this category.";
}
