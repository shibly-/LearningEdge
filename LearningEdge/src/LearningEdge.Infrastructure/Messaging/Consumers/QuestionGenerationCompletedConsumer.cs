using LearningEdge.Application.Models.Commands.Trainings;
using LearningEdge.Messaging.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;

namespace LearningEdge.Infrastructure.Messaging.Consumers;

public sealed class QuestionGenerationCompletedConsumer : IConsumer<IQuestionGenerationCompleted>
{
    private readonly ILogger<QuestionGenerationCompletedConsumer> _logger;
    private readonly IMediator _mediator;


    public QuestionGenerationCompletedConsumer(ILogger<QuestionGenerationCompletedConsumer> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IQuestionGenerationCompleted> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received notification on question generation for Prompt Request Id:{PromptRequestId}", message.PromptRequestId);

        //await _mediator.Send(new UpdateTrainingFileExtractCompletedCommand(message.PromptRequestId, message.TrainingId, message.IsSuccessful, message.FailedReason));
        await Task.Delay(1000);
        await Task.CompletedTask;

        _logger.LogInformation("Updated question generation status for Prompt Request Id:{PromptRequestId}", message.PromptRequestId);
    }
}