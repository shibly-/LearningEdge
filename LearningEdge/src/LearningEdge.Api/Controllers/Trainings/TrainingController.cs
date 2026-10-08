using Asp.Versioning;
using LearningEdge.Api.Extensions;
using LearningEdge.Application.Common.Files;
using LearningEdge.Application.Models.Commands.Trainings;
using LearningEdge.Application.Models.DTOs;
using LearningEdge.Application.Models.Queries.Trainings;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Controllers.Trainings;

[ApiController]
[Route("api/v{version:apiVersion}/category/{categoryId:guid}/training")]
[ApiVersion("1.0")]
public class TrainingController : ControllerBase
{
    // Room for the multipart boundaries and form fields on top of the file bytes.
    private const long MaxUploadRequestBytes = TrainingFileRules.MaxTotalBytes + 1024 * 1024;

    private readonly IMediator _mediator;

    public TrainingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // POST: api/v1/category/{categoryId}/training
    [HttpPost]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Create a training in a category")]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<Guid>> Create(
        Guid categoryId,
        [FromBody] CreateTrainingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { CategoryId = categoryId }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        RouteData.Values.TryGetValue("version", out var version);
        return CreatedAtAction(nameof(GetById), new { version, categoryId, id = result.Data }, result.Data);
    }

    // PUT: api/v1/category/{categoryId}/training/{id}
    [HttpPut("{id:guid}")]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Update a training")]
    [EndpointDescription("Replaces the name, description and active flag of a training in the category.")]
    [ProducesResponseType(typeof(TrainingDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TrainingDTO>> Update(
        Guid categoryId,
        Guid id,
        [FromBody] UpdateTrainingCommand command,
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(command with { CategoryId = categoryId, Id = id }, cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // POST: api/v1/category/{categoryId}/training/{id}/files
    [HttpPost("{id:guid}/files")]
    [MapToApiVersion("1.0")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(MaxUploadRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxUploadRequestBytes)]
    [EndpointSummary("Upload files to a training")]
    [EndpointDescription(
        "Uploads one or more PDF, DOCX or TXT files (form field 'files'). " +
        "'uploadedByUserId' must be an OrgAdmin of the training's organization or a SysAdmin.")]
    [ProducesResponseType(typeof(IReadOnlyList<TrainingFileDTO>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TrainingFileDTO>>> UploadFiles(
        Guid categoryId,
        Guid id,
        [FromForm] Guid uploadedByUserId,
        [FromForm] List<IFormFile> files,
        CancellationToken cancellationToken)
    {
        var uploads = files
            .Select(file => new TrainingFileUpload(file.FileName, file.ContentType, file.Length, file.OpenReadStream()))
            .ToList();

        try
        {
            var result = await _mediator.Send(
                new UploadTrainingFilesCommand(categoryId, id, uploadedByUserId, uploads),
                cancellationToken);
            if (!result.Success)
            {
                return result.ToErrorResult();
            }

            RouteData.Values.TryGetValue("version", out var version);
            return CreatedAtAction(nameof(GetById), new { version, categoryId, id }, result.Data);
        }
        finally
        {
            foreach (var upload in uploads)
            {
                await upload.Content.DisposeAsync();
            }
        }
    }

    // GET: api/v1/category/{categoryId}/training
    [HttpGet]
    [MapToApiVersion("1.0")]
    [EndpointSummary("Get the trainings of a category")]
    [ProducesResponseType(typeof(IReadOnlyList<TrainingDTO>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TrainingDTO>>> GetAll(Guid categoryId, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTrainingsQuery(categoryId), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }

    // GET: api/v1/category/{categoryId}/training/{id}
    [HttpGet("{id:guid}")]
    [MapToApiVersion("1.0")]
    [ProducesResponseType(typeof(TrainingDTO), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TrainingDTO>> GetById(Guid categoryId, Guid id, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTrainingByIdQuery(categoryId, id), cancellationToken);
        if (!result.Success)
        {
            return result.ToErrorResult();
        }

        return Ok(result.Data);
    }
}
