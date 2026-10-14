using Asp.Versioning;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Models.Commands.PromptRequests;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.PromptRequests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Controllers.QuestionBanks;

[ApiController]
[Route("api/v{version:apiVersion}/training/{trainingId:guid}/question-bank/{questionBankId:guid}/prompt-request")]
[ApiVersion("1.0")]
public class PromptRequestController : ControllerBase
{
    private readonly IMediator _mediator;

    public PromptRequestController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/training/{trainingId}/question-bank/{questionBankId}/prompt-request
    [HttpPost]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Submit a prompt and publish the question-generate event")]
    [EndpointDescription(
        "Stores a PromptRequest and publishes it to the question-generate RabbitMQ exchange.")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> Create(
        Guid trainingId,
        Guid questionBankId,
        [FromBody] SubmitPromptRequestCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command with { TrainingId = trainingId, QuestionBankId = questionBankId },
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    // GET: api/v1/training/{trainingId}/question-bank/{questionBankId}/prompt-request
    [HttpGet]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get prompt requests for a question bank")]
    [ProducesResponseType(typeof(IReadOnlyList<PromptRequestDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<PromptRequestDTO>>> GetAll(
        Guid trainingId,
        Guid questionBankId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetPromptRequestsByQuestionBankQuery(trainingId, questionBankId),
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }
}
