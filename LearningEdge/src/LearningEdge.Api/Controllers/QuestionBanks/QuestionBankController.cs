using Asp.Versioning;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Models.Commands.QuestionBanks;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.QuestionBanks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Controllers.QuestionBanks;

[ApiController]
[Route("api/v{version:apiVersion}/training/{trainingId:guid}/question-bank")]
[ApiVersion("1.0")]
public class QuestionBankController : ControllerBase
{
    private readonly IMediator _mediator;

    public QuestionBankController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/training/{trainingId}/question-bank
    [HttpPost]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Create a question bank for a training")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create(
        Guid trainingId,
        [FromBody] CreateQuestionBankCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { TrainingId = trainingId }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        RouteData.Values.TryGetValue("version", out var version);
        return CreatedAtAction(nameof(GetById), new { version, trainingId, id = result.Data }, result.Data);
    }

    // PUT: api/v1/training/{trainingId}/question-bank/{id}
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Update a question bank title and active flag")]
    [ProducesResponseType(typeof(QuestionBankDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<QuestionBankDTO>> Update(
        Guid trainingId,
        Guid id,
        [FromBody] UpdateQuestionBankCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { TrainingId = trainingId, Id = id }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/training/{trainingId}/question-bank
    [HttpGet]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get the question banks of a training")]
    [ProducesResponseType(typeof(IReadOnlyList<QuestionBankDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<QuestionBankDTO>>> GetAll(Guid trainingId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQuestionBanksByTrainingQuery(trainingId), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/training/{trainingId}/question-bank/{id}
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(QuestionBankDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<QuestionBankDTO>> GetById(Guid trainingId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetQuestionBankByIdQuery(trainingId, id), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/question-bank
    [HttpGet("/api/v{version:apiVersion}/question-bank")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get every active question bank")]
    [ProducesResponseType(typeof(IReadOnlyList<QuestionBankDTO>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<QuestionBankDTO>>> GetActive(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetActiveQuestionBanksQuery(), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }
}
