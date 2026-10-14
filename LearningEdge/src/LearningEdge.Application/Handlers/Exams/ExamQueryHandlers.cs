using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Exams;
using LearningEdge.Application.Questions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.Exams;

public class ExamQueryHandlers :
    IRequestHandler<GetExamsByTrainingQuery, Result<IReadOnlyList<ExamDTO>>>,
    IRequestHandler<GetExamByIdQuery, Result<ExamDTO>>,
    IRequestHandler<GetExamQuestionsQuery, Result<IReadOnlyList<QuestionDTO>>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;

    public ExamQueryHandlers(IApplicationDbContext appDbContext, IMapper mapper)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
    }

    public async Task<Result<IReadOnlyList<ExamDTO>>> Handle(GetExamsByTrainingQuery request, CancellationToken cancellationToken)
    {
        var trainingExists = await _appDbContext.Trainings
            .AnyAsync(training => training.Id == request.TrainingId, cancellationToken);

        if (!trainingExists)
        {
            return Result<IReadOnlyList<ExamDTO>>.NotFound($"No training found with Id {request.TrainingId}.");
        }

        var exams = await _appDbContext.Exams
            .AsNoTracking()
            .Where(exam => exam.TrainingId == request.TrainingId)
            .OrderBy(exam => exam.Title)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<ExamDTO>>.Ok(_mapper.Map<List<ExamDTO>>(exams));
    }

    public async Task<Result<ExamDTO>> Handle(GetExamByIdQuery request, CancellationToken cancellationToken)
    {
        var exam = await _appDbContext.Exams
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.Id && candidate.TrainingId == request.TrainingId,
                cancellationToken);

        return exam is null
            ? Result<ExamDTO>.NotFound($"No exam found with Id {request.Id} on training {request.TrainingId}.")
            : Result<ExamDTO>.Ok(_mapper.Map<ExamDTO>(exam));
    }

    public async Task<Result<IReadOnlyList<QuestionDTO>>> Handle(
        GetExamQuestionsQuery request,
        CancellationToken cancellationToken)
    {
        var examExists = await _appDbContext.Exams
            .AnyAsync(
                exam => exam.Id == request.ExamId && exam.TrainingId == request.TrainingId,
                cancellationToken);

        if (!examExists)
        {
            return Result<IReadOnlyList<QuestionDTO>>.NotFound(
                $"No exam found with Id {request.ExamId} on training {request.TrainingId}.");
        }

        var questionIds = await _appDbContext.ExamQuestions
            .AsNoTracking()
            .Where(link => link.ExamId == request.ExamId)
            .Select(link => link.QuestionId)
            .ToListAsync(cancellationToken);

        var questions = await _appDbContext.Questions
            .AsNoTracking()
            .Where(question => questionIds.Contains(question.Id))
            .OrderBy(question => question.Text)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<QuestionDTO>>.Ok(questions.Select(QuestionEditor.ToDto).ToList());
    }
}
