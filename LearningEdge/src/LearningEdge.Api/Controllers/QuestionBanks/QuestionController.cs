using Asp.Versioning;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Models.Commands.Questions;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Questions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Controllers.QuestionBanks;

[ApiController]
[Route("api/v{version:apiVersion}/training/{trainingId:guid}/question-bank/{questionBankId:guid}/question")]
[ApiVersion("1.0")]
public class QuestionController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestionController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/training/{trainingId}/question-bank/{questionBankId}/question
    [HttpPost]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Add a question to a question bank")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Guid>> Create(
        Guid trainingId,
        Guid questionBankId,
        [FromBody] CreateQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command with { TrainingId = trainingId, QuestionBankId = questionBankId },
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        RouteData.Values.TryGetValue("version", out var version);
        return CreatedAtAction(
            nameof(GetById),
            new { version, trainingId, questionBankId, id = result.Data },
            result.Data);
    }

    // PUT: api/v1/training/{trainingId}/question-bank/{questionBankId}/question/{id}
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Update a question")]
    [EndpointDescription("Replaces the text, score, answer and active flag. The question type stays the same.")]
    [ProducesResponseType(typeof(QuestionDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDTO>> Update(
        Guid trainingId,
        Guid questionBankId,
        Guid id,
        [FromBody] UpdateQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command with { TrainingId = trainingId, QuestionBankId = questionBankId, Id = id },
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/training/{trainingId}/question-bank/{questionBankId}/question?activeOnly=
    [HttpGet]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get questions in a question bank")]
    [ProducesResponseType(typeof(IReadOnlyList<QuestionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<QuestionDTO>>> GetAll(
        Guid trainingId,
        Guid questionBankId,
        [FromQuery] bool activeOnly,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetQuestionsByQuestionBankQuery(trainingId, questionBankId, activeOnly),
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/training/{trainingId}/question-bank/{questionBankId}/question/{id}
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QuestionDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionDTO>> GetById(
        Guid trainingId,
        Guid questionBankId,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GetQuestionByIdQuery(trainingId, questionBankId, id),
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }
}
