using Microsoft.AspNetCore.Mvc;
using Mythra.Domain.Common;

namespace Mythra.Api.Common;

public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result) =>
        result.IsSuccess
            ? new OkObjectResult(result.Value)
            : ToProblem(result.Error);

    public static IActionResult ToActionResult(this Result result) =>
        result.IsSuccess ? new NoContentResult() : ToProblem(result.Error);

    public static IActionResult ToCreated<T>(this Result<T> result, string locationFormat) =>
        result.IsSuccess
            ? new CreatedResult(string.Format(locationFormat, result.Value), result.Value)
            : ToProblem(result.Error);

    private static IActionResult ToProblem(Error error)
    {
        var status = error.Code switch
        {
            "not_found" => StatusCodes.Status404NotFound,
            "validation" => StatusCodes.Status400BadRequest,
            "unauthorized" => StatusCodes.Status401Unauthorized,
            "conflict" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };
        var problem = new ProblemDetails
        {
            Title = error.Code,
            Detail = error.Message,
            Status = status,
            Type = $"https://mythra.local/problems/{error.Code}",
        };
        return new ObjectResult(problem) { StatusCode = status };
    }
}
