using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Mythra.Api.Common;

public sealed class SwaggerBearerOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var hasAuthorize = context.MethodInfo.DeclaringType?
            .GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any() == true
            || context.MethodInfo.GetCustomAttributes(true).OfType<AuthorizeAttribute>().Any();
        if (!hasAuthorize) return;

        operation.Responses ??= [];
        if (!operation.Responses.ContainsKey("401"))
            operation.Responses.Add("401", new OpenApiResponse { Description = "Unauthorized" });
    }
}
