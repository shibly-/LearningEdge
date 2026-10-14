using Asp.Versioning;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Models.Commands.Exams;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Exams;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Controllers.Exams;

[ApiController]
[Route("api/v{version:apiVersion}/training/{trainingId:guid}/exam")]
[ApiVersion("1.0")]
public class ExamController : ControllerBase
{
    private readonly IMediator _mediator;

    public ExamController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/training/{trainingId}/exam
    [HttpPost]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Create an exam for a training")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create(
        Guid trainingId,
        [FromBody] CreateExamCommand command,
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

    // PUT: api/v1/training/{trainingId}/exam/{id}
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Update an exam title and active flag")]
    [ProducesResponseType(typeof(ExamDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ExamDTO>> Update(
        Guid trainingId,
        Guid id,
        [FromBody] UpdateExamCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { TrainingId = trainingId, Id = id }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/training/{trainingId}/exam
    [HttpGet]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get the exams of a training")]
    [ProducesResponseType(typeof(IReadOnlyList<ExamDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ExamDTO>>> GetAll(Guid trainingId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExamsByTrainingQuery(trainingId), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/training/{trainingId}/exam/{id}
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(ExamDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ExamDTO>> GetById(Guid trainingId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExamByIdQuery(trainingId, id), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/training/{trainingId}/exam/{examId}/question
    [HttpGet("{examId:guid}/question")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get questions attached to an exam")]
    [ProducesResponseType(typeof(IReadOnlyList<QuestionDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<QuestionDTO>>> GetQuestions(
        Guid trainingId,
        Guid examId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetExamQuestionsQuery(trainingId, examId), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // POST: api/v1/training/{trainingId}/exam/{examId}/question
    [HttpPost("{examId:guid}/question")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Attach a question bank question to an exam")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> AddQuestion(
        Guid trainingId,
        Guid examId,
        [FromBody] AddExamQuestionCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            command with { TrainingId = trainingId, ExamId = examId },
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return StatusCode(StatusCodes.Status201Created, result.Data);
    }

    // DELETE: api/v1/training/{trainingId}/exam/{examId}/question/{questionId}
    [HttpDelete("{examId:guid}/question/{questionId:guid}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Remove a question from an exam")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveQuestion(
        Guid trainingId,
        Guid examId,
        Guid questionId,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RemoveExamQuestionCommand(trainingId, examId, questionId),
            cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return NoContent();
    }
}
