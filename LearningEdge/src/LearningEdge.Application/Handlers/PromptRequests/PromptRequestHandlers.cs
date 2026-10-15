using AutoMapper;
using LearningEdge.Application.Common.Results;
using LearningEdge.Application.Events;
using LearningEdge.Application.Interfaces;
using LearningEdge.Application.Models.Commands.PromptRequests;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.PromptRequests;
using LearningEdge.Domain.Entities.Trainings;
using LearningEdge.Messaging.Contracts.Events;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace LearningEdge.Application.Handlers.PromptRequests;

public class PromptRequestHandlers :
    IRequestHandler<SubmitPromptRequestCommand, Result<Guid>>,
    IRequestHandler<GetPromptRequestsByQuestionBankQuery, Result<IReadOnlyList<PromptRequestDTO>>>
{
    private readonly IApplicationDbContext _appDbContext;
    private readonly IMapper _mapper;
    private readonly IPublishEndpoint? _publishEndpoint;

    public PromptRequestHandlers(IApplicationDbContext appDbContext, IMapper mapper, IPublishEndpoint? publishEndpoint = null)
    {
        _appDbContext = appDbContext;
        _mapper = mapper;
        _publishEndpoint = publishEndpoint;
    }

    public async Task<Result<Guid>> Handle(SubmitPromptRequestCommand request, CancellationToken cancellationToken)
    {
        if (_publishEndpoint is null)
        {
            return Result<Guid>.Fail("Question generation is not available because the message bus is not configured.");
        }

        var bank = await _appDbContext.QuestionBanks
            .AsNoTracking()
            .FirstOrDefaultAsync(
                candidate => candidate.Id == request.QuestionBankId && candidate.TrainingId == request.TrainingId,
                cancellationToken);

        if (bank is null)
        {
            return Result<Guid>.NotFound(
                $"No question bank found with Id {request.QuestionBankId} on training {request.TrainingId}.");
        }

        var prompt = new PromptRequest(
            request.TrainingId,
            request.QuestionBankId,
            request.PromptText.Trim(),
            request.QuestionType,
            request.QuestionCount);

        _appDbContext.PromptRequests.Add(prompt);
        await _appDbContext.SaveChangesAsync(cancellationToken);

        IQuestionGenerationSubmitted message = new QuestionGenerationSubmitted(
            prompt.Id,
            prompt.TrainingId,
            prompt.QuestionBankId,
            prompt.PromptText,
            prompt.QuestionType.ToString(),
            prompt.QuestionCount,
            prompt.RequestedOn,
            prompt.Status.ToString()
        );

        using var publishTimeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        publishTimeout.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            await _publishEndpoint.Publish(message, publishTimeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Result<Guid>.Fail(
                "The prompt request was saved, but the question-generate event could not be published.");
        }
        catch (Exception)
        {
            return Result<Guid>.Fail(
                "The prompt request was saved, but the question-generate event could not be published.");
        }

        return Result<Guid>.Ok(prompt.Id);
    }

    public async Task<Result<IReadOnlyList<PromptRequestDTO>>> Handle(
        GetPromptRequestsByQuestionBankQuery request,
        CancellationToken cancellationToken)
    {
        var bankExists = await _appDbContext.QuestionBanks
            .AnyAsync(
                bank => bank.Id == request.QuestionBankId && bank.TrainingId == request.TrainingId,
                cancellationToken);

        if (!bankExists)
        {
            return Result<IReadOnlyList<PromptRequestDTO>>.NotFound(
                $"No question bank found with Id {request.QuestionBankId} on training {request.TrainingId}.");
        }

        var prompts = await _appDbContext.PromptRequests
            .AsNoTracking()
            .Where(prompt => prompt.QuestionBankId == request.QuestionBankId && prompt.TrainingId == request.TrainingId)
            .OrderByDescending(prompt => prompt.RequestedOn)
            .ToListAsync(cancellationToken);

        return Result<IReadOnlyList<PromptRequestDTO>>.Ok(_mapper.Map<List<PromptRequestDTO>>(prompts));
    }
}
