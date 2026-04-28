using Serilog.Context;

namespace Mythra.Api.Middleware;

public sealed class CorrelationIdMiddleware(RequestDelegate next)
{
    private const string Header = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext ctx)
    {
        var id = ctx.Request.Headers.TryGetValue(Header, out var existing) && !string.IsNullOrEmpty(existing)
            ? existing.ToString()
            : Guid.NewGuid().ToString("N");
        ctx.Response.Headers[Header] = id;
        ctx.TraceIdentifier = id;
        using (LogContext.PushProperty("CorrelationId", id))
        {
            await next(ctx);
        }
    }
}
