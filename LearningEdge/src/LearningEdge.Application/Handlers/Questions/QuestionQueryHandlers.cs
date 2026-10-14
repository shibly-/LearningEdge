using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Questions;
using LearningEdge.Application.Questions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Questions;

public class QuestionQueryHandlers :
    IRequestHandler<GetQuestionsByQuestionBankQuery, Result<IReadOnlyList<QuestionDTO>>>,
    IRequestHandler<GetQuestionByIdQuery, Result<QuestionDTO>>
{
    private readonly IApplicationDbContext _appDbContext;

    public QuestionQueryHandlers(IApplicationDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<IReadOnlyList<QuestionDTO>>> Handle(
        GetQuestionsByQuestionBankQuery request,
        CancellationToken cancellationToken)
    {
        var bankExists = await _appDbContext.QuestionBanks
            .AnyAsync(
                bank => bank.Id == request.QuestionBankId && bank.TrainingId == request.TrainingId,
                cancellationToken);

        if (!bankExists)
        {
            return Result<IReadOnlyList<QuestionDTO>>.NotFound(
                $"No question bank found with Id {request.QuestionBankId} on training {request.TrainingId}.");
        }

        var questions = await _appDbContext.Questions
            .AsNoTracking()
            .Where(question => question.QuestionBankId == request.QuestionBankId
                && (!request.ActiveOnly || question.IsActive))
            .OrderByDescending(question => question.CreatedAt)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<QuestionDTO>>.Ok(questions.Select(QuestionEditor.ToDto).ToList());
    }

    public async Task<Result<QuestionDTO>> Handle(GetQuestionByIdQuery request, CancellationToken cancellationToken)
    {
        var question = await _appDbContext.Questions
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.QuestionBankId == request.QuestionBankId,
                cancellationToken);

        if (question is null)
        {
            return Result<QuestionDTO>.NotFound(
                $"No question found with Id {request.Id} in question bank {request.QuestionBankId}.");
        }

        var bankExists = await _appDbContext.QuestionBanks
            .AnyAsync(
                bank => bank.Id == request.QuestionBankId && bank.TrainingId == request.TrainingId,
                cancellationToken);

        if (!bankExists)
        {
            return Result<QuestionDTO>.NotFound(
                $"No question bank found with Id {request.QuestionBankId} on training {request.TrainingId}.");
        }

        return Result<QuestionDTO>.Ok(QuestionEditor.ToDto(question));
    }
}
