using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Trainings;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Trainings;

public class TrainingQueryHandlers :
    IRequestHandler<GetTrainingByIdQuery, Result<TrainingDTO>>,
    IRequestHandler<GetTrainingsQuery, Result<IReadOnlyList<TrainingDTO>>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public TrainingQueryHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<TrainingDTO>> Handle(GetTrainingByIdQuery request, CancellationToken cancellationToken)
    {
        var training = await _appDbContext.Trainings
            .AsNoTracking()
            .Include(candidate => candidate.Files.OrderBy(file => file.CreatedAt))
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.CategoryId == request.CategoryId,
                cancellationToken);

        return training is null
            ? Result<TrainingDTO>.NotFound($"No training found with Id {request.Id} in category {request.CategoryId}.")
            : Result<TrainingDTO>.Ok(_mapper.Map<TrainingDTO>(training));
    }

    public async Task<Result<IReadOnlyList<TrainingDTO>>> Handle(GetTrainingsQuery request, CancellationToken cancellationToken)
    {
        var categoryExists = await _appDbContext.Categories
            .AnyAsync(category => category.Id == request.CategoryId, cancellationToken);

        if (!categoryExists)
        {
            return Result<IReadOnlyList<TrainingDTO>>.NotFound($"No category found with Id {request.CategoryId}.");
        }

        var trainings = await _appDbContext.Trainings
            .AsNoTracking()
            .Include(training => training.Files.OrderBy(file => file.CreatedAt))
            .Where(training => training.CategoryId == request.CategoryId)
            .OrderBy(training => training.Name)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<TrainingDTO>>.Ok(_mapper.Map<List<TrainingDTO>>(trainings));
    }
}
