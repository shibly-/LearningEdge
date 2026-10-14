using LearningEdge.Application.Models.Commands.Trainings;
using LearningEdge.Messaging.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.Build.Framework;
using Microsoft.Extensions.Logging;

namespace LearningEdge.Infrastructure.Messaging.Consumers;

public sealed class ExtractRequestCompletedConsumer : IConsumer<IExtractRequestCompleted>
{
    private readonly ILogger<ExtractRequestCompletedConsumer> _logger;
    private readonly IMediator _mediator;


    public ExtractRequestCompletedConsumer(ILogger<ExtractRequestCompletedConsumer> logger, IMediator mediator)
    {
        _logger = logger;
        _mediator = mediator;
    }

    public async Task Consume(ConsumeContext<IExtractRequestCompleted> context)
    {
        var message = context.Message;
        _logger.LogInformation("Received notification on extract processing for File Id:{FileId}", message.FileId);

        await _mediator.Send(new UpdateTrainingFileExtractCompletedCommand(message.FileId, message.TrainingId, message.IsSuccessful, message.FailedReason));

        _logger.LogInformation("Updated file extraction status for File Id:{FileId}", message.FileId);
    }
}
