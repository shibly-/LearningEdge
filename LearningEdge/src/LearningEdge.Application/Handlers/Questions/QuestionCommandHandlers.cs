using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Questions;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Questions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Questions;

public class QuestionCommandHandlers :
    IRequestHandler<CreateQuestionCommand, Result<Guid>>,
    IRequestHandler<UpdateQuestionCommand, Result<QuestionDTO>>
{
    private readonly IApplicationDbContext _appDbContext;

    public QuestionCommandHandlers(IApplicationDbContext appDbContext)
    {
        _appDbContext = appDbContext;
    }

    public async Task<Result<Guid>> Handle(CreateQuestionCommand request, CancellationToken cancellationToken)
    {
        var bank = await FindBank(request.TrainingId, request.QuestionBankId, cancellationToken);
        if (bank is null)
        {
            return Result<Guid>.NotFound(
                $"No question bank found with Id {request.QuestionBankId} on training {request.TrainingId}.");
        }

        var draft = QuestionEditor.Draft(
            request.QuestionType,
            request.Text,
            request.Score,
            request.IsActive,
            request.Options,
            request.CorrectOptions,
            request.CorrectValue);

        if (!draft.Success || draft.Data is null)
        {
            return Result<Guid>.Validation(draft.Error);
        }

        var question = QuestionEditor.Create(request.QuestionType, request.QuestionBankId, draft.Data);
        _appDbContext.Questions.Add(question);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Ok(question.Id);
    }

    public async Task<Result<QuestionDTO>> Handle(UpdateQuestionCommand request, CancellationToken cancellationToken)
    {
        var bank = await FindBank(request.TrainingId, request.QuestionBankId, cancellationToken);
        if (bank is null)
        {
            return Result<QuestionDTO>.NotFound(
                $"No question bank found with Id {request.QuestionBankId} on training {request.TrainingId}.");
        }

        var question = await _appDbContext.Questions
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.QuestionBankId == request.QuestionBankId,
                cancellationToken);

        if (question is null)
        {
            return Result<QuestionDTO>.NotFound(
                $"No question found with Id {request.Id} in question bank {request.QuestionBankId}.");
        }

        var draft = QuestionEditor.Draft(
            QuestionEditor.TypeOf(question),
            request.Text,
            request.Score,
            request.IsActive,
            request.Options,
            request.CorrectOptions,
            request.CorrectValue);

        if (!draft.Success || draft.Data is null)
        {
            return Result<QuestionDTO>.Validation(draft.Error);
        }

        var problem = QuestionEditor.Apply(question, draft.Data);
        if (problem is not null)
        {
            return Result<QuestionDTO>.Validation(problem);
        }

        await _appDbContext.SaveChangesAsync(cancellationToken);
        return Result<QuestionDTO>.Ok(QuestionEditor.ToDto(question));
    }

    private async Task<Domain.Entities.Questions.QuestionBank?> FindBank(
        Guid trainingId,
        Guid questionBankId,
        CancellationToken cancellationToken) =>
        await _appDbContext.QuestionBanks.FirstOrDefaultAsync(
            bank => bank.Id == questionBankId && bank.TrainingId == trainingId,
            cancellationToken);
}
