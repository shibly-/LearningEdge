using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.QuestionBanks;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.QuestionBanks;

public class QuestionBankQueryHandlers :
    IRequestHandler<GetQuestionBanksByTrainingQuery, Result<IReadOnlyList<QuestionBankDTO>>>,
    IRequestHandler<GetQuestionBankByIdQuery, Result<QuestionBankDTO>>,
    IRequestHandler<GetActiveQuestionBanksQuery, Result<IReadOnlyList<QuestionBankDTO>>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public QuestionBankQueryHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<QuestionBankDTO>>> Handle(
        GetQuestionBanksByTrainingQuery request,
        CancellationToken cancellationToken)
    {
        var trainingExists = await _appDbContext.Trainings
            .AnyAsync(training => training.Id == request.TrainingId, cancellationToken);

        if (!trainingExists)
        {
            return Result<IReadOnlyList<QuestionBankDTO>>.NotFound($"No training found with Id {request.TrainingId}.");
        }

        var banks = await _appDbContext.QuestionBanks
            .AsNoTracking()
            .Where(bank => bank.TrainingId == request.TrainingId)
            .OrderBy(bank => bank.Title)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<QuestionBankDTO>>.Ok(_mapper.Map<List<QuestionBankDTO>>(banks));
    }

    public async Task<Result<QuestionBankDTO>> Handle(GetQuestionBankByIdQuery request, CancellationToken cancellationToken)
    {
        var bank = await _appDbContext.QuestionBanks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.TrainingId == request.TrainingId,
                cancellationToken);

        return bank is null
            ? Result<QuestionBankDTO>.NotFound($"No question bank found with Id {request.Id} on training {request.TrainingId}.")
            : Result<QuestionBankDTO>.Ok(_mapper.Map<QuestionBankDTO>(bank));
    }

    public async Task<Result<IReadOnlyList<QuestionBankDTO>>> Handle(
        GetActiveQuestionBanksQuery request,
        CancellationToken cancellationToken)
    {
        var banks = await _appDbContext.QuestionBanks
            .AsNoTracking()
            .Where(bank => bank.IsActive)
            .OrderBy(bank => bank.Title)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<QuestionBankDTO>>.Ok(_mapper.Map<List<QuestionBankDTO>>(banks));
    }
}
