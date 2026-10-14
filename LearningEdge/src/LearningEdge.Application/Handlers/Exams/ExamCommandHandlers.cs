using AutoMapper;
using LearningEdge.Application.Common.Exceptions;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.Exams;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Domain.Entities.Exams;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Exams;

public class ExamCommandHandlers :
    IRequestHandler<CreateExamCommand, Result<Guid>>,
    IRequestHandler<UpdateExamCommand, Result<ExamDTO>>,
    IRequestHandler<AddExamQuestionCommand, Result<Guid>>,
    IRequestHandler<RemoveExamQuestionCommand, Result<Guid>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public ExamCommandHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<Guid>> Handle(CreateExamCommand request, CancellationToken cancellationToken)
    {
        var trainingExists = await _appDbContext.Trainings
            .AnyAsync(training => training.Id == request.TrainingId, cancellationToken);

        if (!trainingExists)
        {
            return Result<Guid>.NotFound($"No training found with Id {request.TrainingId}.");
        }

        var userExists = await _appDbContext.Users
            .AnyAsync(user => user.Id == request.PreparedByUserId, cancellationToken);

        if (!userExists)
        {
            return Result<Guid>.NotFound($"No user found with Id {request.PreparedByUserId}.");
        }

        var title = request.Title.Trim();
        if (await TitleTaken(request.TrainingId, title, null, cancellationToken))
        {
            return Result<Guid>.Conflict(TitleTakenMessage(title));
        }

        var exam = new Exam(title, request.TrainingId, request.PreparedByUserId, request.IsActive);
        _appDbContext.Exams.Add(exam);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<Guid>.Conflict(TitleTakenMessage(title));
        }

        return Result<Guid>.Ok(exam.Id);
    }

    public async Task<Result<ExamDTO>> Handle(UpdateExamCommand request, CancellationToken cancellationToken)
    {
        var exam = await _appDbContext.Exams
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.TrainingId == request.TrainingId,
                cancellationToken);

        if (exam is null)
        {
            return Result<ExamDTO>.NotFound($"No exam found with Id {request.Id} on training {request.TrainingId}.");
        }

        var title = request.Title.Trim();
        if (await TitleTaken(request.TrainingId, title, request.Id, cancellationToken))
        {
            return Result<ExamDTO>.Conflict(TitleTakenMessage(title));
        }

        exam.Update(title, request.IsActive);

        try
        {
            await _appDbContext.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            return Result<ExamDTO>.Conflict(TitleTakenMessage(title));
        }

        return Result<ExamDTO>.Ok(_mapper.Map<ExamDTO>(exam));
    }

    public async Task<Result<Guid>> Handle(AddExamQuestionCommand request, CancellationToken cancellationToken)
    {
        var exam = await _appDbContext.Exams
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.ExamId && candidate.TrainingId == request.TrainingId,
                cancellationToken);

        if (exam is null)
        {
            return Result<Guid>.NotFound($"No exam found with Id {request.ExamId} on training {request.TrainingId}.");
        }

        var bankIsActive = await _appDbContext.QuestionBanks
            .AnyAsync(bank => bank.Id == request.QuestionBankId && bank.IsActive, cancellationToken);

        if (!bankIsActive)
        {
            return Result<Guid>.Validation("Choose an active question bank.");
        }

        var questionIsActive = await _appDbContext.Questions
            .AnyAsync(
                question => question.Id == request.QuestionId
                    && question.QuestionBankId == request.QuestionBankId
                    && question.IsActive,
                cancellationToken);

        if (!questionIsActive)
        {
            return Result<Guid>.Validation("Only an active question from the selected question bank can be added.");
        }

        var alreadyAdded = await _appDbContext.ExamQuestions
            .AnyAsync(
                link => link.ExamId == request.ExamId && link.QuestionId == request.QuestionId,
                cancellationToken);

        if (alreadyAdded)
        {
            return Result<Guid>.Conflict("That question is already on this exam.");
        }

        _appDbContext.ExamQuestions.Add(new ExamQuestion(request.ExamId, request.QuestionId));
        await _appDbContext.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Ok(request.QuestionId);
    }

    public async Task<Result<Guid>> Handle(RemoveExamQuestionCommand request, CancellationToken cancellationToken)
    {
        var examExists = await _appDbContext.Exams
            .AnyAsync(
                exam => exam.Id == request.ExamId && exam.TrainingId == request.TrainingId,
                cancellationToken);

        if (!examExists)
        {
            return Result<Guid>.NotFound($"No exam found with Id {request.ExamId} on training {request.TrainingId}.");
        }

        var link = await _appDbContext.ExamQuestions
            .FirstOrDefaultAsync(
                candidate => candidate.ExamId == request.ExamId && candidate.QuestionId == request.QuestionId,
                cancellationToken);

        if (link is null)
        {
            return Result<Guid>.NotFound($"No question found with Id {request.QuestionId} on exam {request.ExamId}.");
        }

        _appDbContext.ExamQuestions.Remove(link);
        await _appDbContext.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Ok(request.QuestionId);
    }

    private async Task<bool> TitleTaken(Guid trainingId, string title, Guid? exceptId, CancellationToken cancellationToken) =>
        await _appDbContext.Exams.AnyAsync(
            exam => exam.TrainingId == trainingId
                && exam.Title == title
                && (exceptId == null || exam.Id != exceptId),
            cancellationToken);

    private static string TitleTakenMessage(string title) =>
        $"An exam named {title} already exists for this training.";
}
