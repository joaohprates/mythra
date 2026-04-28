using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Mythra.Api.Middleware;

public sealed class ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> log)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext ctx)
    {
        try
        {
            await next(ctx);
        }
        catch (UnauthorizedAccessException ex)
        {
            await WriteProblem(ctx, StatusCodes.Status401Unauthorized, "unauthorized", ex.Message);
        }
        catch (ArgumentException ex)
        {
            await WriteProblem(ctx, StatusCodes.Status400BadRequest, "bad_request", ex.Message);
        }
        catch (KeyNotFoundException ex)
        {
            await WriteProblem(ctx, StatusCodes.Status404NotFound, "not_found", ex.Message);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Unhandled exception while processing {Method} {Path}", ctx.Request.Method, ctx.Request.Path);
            await WriteProblem(ctx, StatusCodes.Status500InternalServerError, "internal_error", "An unexpected error occurred.");
        }
    }

    private static async Task WriteProblem(HttpContext ctx, int status, string code, string detail)
    {
        if (ctx.Response.HasStarted) return;
        ctx.Response.Clear();
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/problem+json";
        var problem = new ProblemDetails
        {
            Title = code,
            Detail = detail,
            Status = status,
            Type = $"https://mythra.local/problems/{code}",
        };
        await ctx.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
