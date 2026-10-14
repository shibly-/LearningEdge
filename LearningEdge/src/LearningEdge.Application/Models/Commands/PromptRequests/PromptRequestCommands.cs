using LearningEdge.Application.Common.Results;
using LearningEdge.Domain.ValueObjects;
using MediatR;

namespace LearningEdge.Application.Models.Commands.PromptRequests;

public record SubmitPromptRequestCommand(
    Guid TrainingId,
    Guid QuestionBankId,
    string PromptText,
    QuestionType QuestionType,
    int QuestionCount
) : IRequest<Result<Guid>>;
