using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Files;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Events;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Trainings;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Messaging.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using static LearningEdge.Domain.Common.Enums;
using static System.Net.WebRequestMethods;

namespace LearningEdge.Application.Handlers.Trainings;

public class TrainingCommandHandlers :
    IRequestHandler<CreateTrainingCommand, Result<Guid>>,
    IRequestHandler<UpdateTrainingCommand, Result<TrainingDTO>>,
    IRequestHandler<UploadTrainingFilesCommand, Result<IReadOnlyList<TrainingFileDTO>>>,
    IRequestHandler<RemoveTrainingFileCommand, Result<Guid>>,
    IRequestHandler<UpdateTrainingInitiateFileExtractCommand, Result<IReadOnlyList<TrainingFileDTO>>>,
    IRequestHandler<UpdateTrainingFileExtractCompletedCommand, Result<bool>>
{
    private readonly IConfiguration _configuration;
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;
    private readonly IFileStorage _fileStorage;
    private readonly IPublishEndpoint? _publishEndpoint;

    public TrainingCommandHandlers(IConfiguration configuration, IApplicationDbContext appDbContext, IMapper mapper, IFileStorage fileStorage, IPublishEndpoint? publishEndpoint = null)
    {
        _configuration = configuration;
        _appDbContext = appDbContext;
        _mapper = mapper;
        _fileStorage = fileStorage;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<Guid>> Handle(CreateTrainingCommand request, CancellationToken cancellationToken)
    {
        var categoryExists = await _appDbContext.Categories
            .AnyAsync(
                category => category.Id == request.CategoryId && category.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (!categoryExists)
        {
            return Result<Guid>.NotFound(
                $"No category found with Id {request.CategoryId} in organization {request.OrganizationId}.");
        }

        var nameTaken = await _appDbContext.Trainings
            .AnyAsync(
                training => training.CategoryId == request.CategoryId && training.Name == request.Name,
                cancellationToken);

        if (nameTaken)
        {
            return Result<Guid>.Conflict(NameTakenMessage(request.Name));
        }

        var training = new Training(
            request.OrganizationId,
            request.CategoryId,
            request.Name,
            request.Description ?? string.Empty,
            request.IsActive);
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
                candidate => candidate.Id == request.Id
                    && candidate.CategoryId == request.CategoryId
                    && candidate.OrganizationId == request.OrganizationId,
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
        var training = await _appDbContext.Trainings
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.TrainingId
                    && candidate.CategoryId == request.CategoryId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (training is null)
        {
            return Result<IReadOnlyList<TrainingFileDTO>>.NotFound(
                $"No training found with Id {request.TrainingId} in category {request.CategoryId}.");
        }

        var uploader = await _appDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == request.UploadedByUserId, cancellationToken);

        var isAdmin = uploader is not null
            && (uploader.Role == UserRole.SysAdmin
                || (uploader.Role == UserRole.OrgAdmin && uploader.OrganizationId == training.OrganizationId));

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
                    file.Content, $"trainings/{training.Id}", extension, cancellationToken);
                storedKeys.Add(key);

                var entity = training.AddFile(
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
        var trainingEntity = await _appDbContext.Trainings
            .Include(candidate => candidate.Files)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.TrainingId
                    && candidate.CategoryId == request.CategoryId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (trainingEntity is null)
        {
            return Result<Guid>.NotFound(
                $"No training found with Id {request.TrainingId} in category {request.CategoryId}.");
        }

        var actor = await _appDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == request.RemovedByUserId, cancellationToken);

        var isAdmin = actor is not null
            && (actor.Role == UserRole.SysAdmin
                || (actor.Role == UserRole.OrgAdmin && actor.OrganizationId == trainingEntity.OrganizationId));

        if (!isAdmin)
        {
            return Result<Guid>.Forbidden(
                "Only an organization admin or system admin can remove training files.");
        }

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

    public async Task<Result<IReadOnlyList<TrainingFileDTO>>> Handle(UpdateTrainingInitiateFileExtractCommand request, CancellationToken cancellationToken)
    {
        var trainingEntity = await _appDbContext.Trainings
            .Include(candidate => candidate.Files)
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.TrainingId
                    && candidate.CategoryId == request.CategoryId
                    && candidate.OrganizationId == request.OrganizationId,
                cancellationToken);

        if (trainingEntity is null)
        {
            return Result<IReadOnlyList<TrainingFileDTO>>.NotFound(
                $"No training found with Id {request.TrainingId} in category {request.CategoryId}.");
        }

        var actor = await _appDbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(user => user.Id == request.RequestedByUserId, cancellationToken);

        var isAdmin = actor is not null
            && (actor.Role == UserRole.SysAdmin
                || (actor.Role == UserRole.OrgAdmin && actor.OrganizationId == trainingEntity.OrganizationId));

        if (!isAdmin)
        {
            return Result<IReadOnlyList<TrainingFileDTO>>.Forbidden(
                "Only an organization admin or system admin can request training extracts.");
        }

        // File extraction flag is enabled
        var file = trainingEntity.FileExtractionIsInitiated(request.FileId);        
        if (file is null)
        {   
            return Result<IReadOnlyList<TrainingFileDTO>>.NotFound(
                $"No file found with File Id {request.FileId} on training {request.TrainingId}.");
        }

        // File will be soft deleted, so that no one can access it and remove it as the extract process will start through a designated microservice.
        if (_publishEndpoint is null)
        {
            return Result<IReadOnlyList<TrainingFileDTO>>.NotFound(
                $"No publish endpoint found for File Id {request.FileId} on training {request.TrainingId}.");
        }

        IExtractRequestSubmitted message = new ExtractRequestSubmitted (
            FileId: request.FileId,
            FileName: file.FileName,
            ContentType: file.ContentType,
            SizeBytes: file.SizeBytes,
            CreatedAt: file.CreatedAt,
            UploadedByUserId: file.UploadedByUserId,
            RequestedByUserId: request.RequestedByUserId,
            RequestedAt: DateTime.Now,
            StorageKey: file.StorageKey
        );

        // Publish the file extract event to RabbitMQ via MassTransit
        await _publishEndpoint.Publish(message, cfg => cfg.SetRoutingKey(_configuration["RabbitMQ:FileExtractRequestEvent"] ?? string.Empty), cancellationToken);

        await _appDbContext.SaveChangesAsync(cancellationToken);

        return Result<IReadOnlyList<TrainingFileDTO>>.Ok(
            _mapper.Map<List<TrainingFileDTO>>(trainingEntity.Files));
    }

    public async Task<Result<bool>> Handle(UpdateTrainingFileExtractCompletedCommand request, CancellationToken cancellationToken)
    {
        string extractFailureMessage = request.IsSuccessful ? string.Empty : $"Training file extract failed for File Id {request.FileId} on training Id {request.TrainingId}.";

        bool isExtracted = request.IsSuccessful;
        bool isFileExtractionFailed = !request.IsSuccessful;

        var fileUpdateCount = await _appDbContext.TrainingFiles
                .Where(file => file.Id == request.FileId)
                .ExecuteUpdateAsync(
                    setters => setters
                        .SetProperty(file => file.IsFileExtracted, isExtracted)
                        .SetProperty(file => file.IsFileExtractionFailed, isFileExtractionFailed)
                        .SetProperty(file => file.FileExtractionFailureMessage, extractFailureMessage)
                        .SetProperty(file => file.UpdatedAt, DateTime.Now),

                    cancellationToken);

        if (fileUpdateCount == 0)
        {
            return Result<bool>.NotFound(
                $"No file found with File Id {request.FileId} on training {request.TrainingId}.");
        }

        return Result<bool>.Ok(true);
    }

    private static string NameTakenMessage(string name) =>
        $"A training named {name} already exists in this category.";
}
