using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Mvc;

namespace ProductRequests.Api.Authorization;

internal sealed class ProblemAuthorizationResultHandler(IProblemDetailsService problemDetailsService)
    : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler _fallback = new();

    public async Task HandleAsync(
        RequestDelegate next,
        HttpContext context,
        AuthorizationPolicy policy,
        PolicyAuthorizationResult authorizeResult)
    {
        if (authorizeResult.Succeeded)
        {
            await _fallback.HandleAsync(next, context, policy, authorizeResult);
            return;
        }

        int status = authorizeResult.Forbidden
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status401Unauthorized;
        string code = authorizeResult.Forbidden ? "RESOURCE_ACCESS_DENIED" : "INVALID_CREDENTIALS";
        context.Response.StatusCode = status;
        var problem = new ProblemDetails
        {
            Type = $"https://product-requests.local/errors/{code.ToLowerInvariant().Replace('_', '-')}",
            Title = authorizeResult.Forbidden ? "Access denied" : "Unauthorized",
            Status = status,
            Detail = authorizeResult.Forbidden
                ? "Access to this resource is denied."
                : "Authentication is required.",
            Instance = context.Request.Path
        };
        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = problem
        });
    }
}
