using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.QuestionBanks;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Questions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.QuestionBanks;

public class QuestionBankCommandHandlers :
    IRequestHandler<CreateQuestionBankCommand, Result<Guid>>,
    IRequestHandler<UpdateQuestionBankCommand, Result<QuestionBankDTO>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public QuestionBankCommandHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<Guid>> Handle(CreateQuestionBankCommand request, CancellationToken cancellationToken)
    {
        var trainingExists = await _appDbContext.Trainings
            .AnyAsync(training => training.Id == request.TrainingId, cancellationToken);

        if (!trainingExists)
        {
            return Result<Guid>.NotFound($"No training found with Id {request.TrainingId}.");
        }

        var title = request.Title.Trim();
        if (await TitleTaken(request.TrainingId, title, null, cancellationToken))
        {
            return Result<Guid>.Conflict(TitleTakenMessage(title));
        }

        var bank = new QuestionBank(title, request.TrainingId, request.IsActive);
        _appDbContext.QuestionBanks.Add(bank);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict(TitleTakenMessage(title));
        }

        return Result<Guid>.Ok(bank.Id);
    }

    public async Task<Result<QuestionBankDTO>> Handle(UpdateQuestionBankCommand request, CancellationToken cancellationToken)
    {
        var bank = await _appDbContext.QuestionBanks
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.TrainingId == request.TrainingId,
                cancellationToken);

        if (bank is null)
        {
            return Result<QuestionBankDTO>.NotFound(
                $"No question bank found with Id {request.Id} on training {request.TrainingId}.");
        }

        var title = request.Title.Trim();
        if (await TitleTaken(request.TrainingId, title, request.Id, cancellationToken))
        {
            return Result<QuestionBankDTO>.Conflict(TitleTakenMessage(title));
        }

        bank.Update(title, request.IsActive);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<QuestionBankDTO>.Conflict(TitleTakenMessage(title));
        }

        return Result<QuestionBankDTO>.Ok(_mapper.Map<QuestionBankDTO>(bank));
    }

    private async Task<bool> TitleTaken(Guid trainingId, string title, Guid? exceptId, CancellationToken cancellationToken) =>
        await _appDbContext.QuestionBanks.AnyAsync(
            bank => bank.TrainingId == trainingId
                && bank.Title == title
                && (exceptId == null || bank.Id != exceptId),
            cancellationToken);

    private static string TitleTakenMessage(string title) =>
        $"A question bank named {title} already exists for this training.";
}
