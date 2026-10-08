using LearningEdge.Application.Common.Results;
using Microsoft.AspNetCore.Mvc;

namespace LearningEdge.Api.Extensions;

public static class ResultHttpExtensions
{
    public static ActionResult ToErrorResult<T>(this Result<T> result)
    {
        var statusCode = result.Kind switch
        {
            ResultErrorKind.Validation => StatusCodes.Status400BadRequest,
            ResultErrorKind.NotFound => StatusCodes.Status404NotFound,
            ResultErrorKind.Conflict => StatusCodes.Status409Conflict,
            ResultErrorKind.Forbidden => StatusCodes.Status403Forbidden,
            _ => StatusCodes.Status500InternalServerError
        };

        var title = result.Kind switch
        {
            ResultErrorKind.Validation => "Validation failed",
            ResultErrorKind.NotFound => "Not found",
            ResultErrorKind.Conflict => "Conflict",
            ResultErrorKind.Forbidden => "Forbidden",
            _ => "Request failed"
        };

        return new ObjectResult(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = result.Error
        })
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    public static CreatedAtActionResult ToCreatedResult(this ControllerBase controller, string actionName, Guid? id)
    {
        controller.RouteData.Values.TryGetValue("version", out var version);
        return controller.CreatedAtAction(actionName, new { version, id }, id);
    }
}
