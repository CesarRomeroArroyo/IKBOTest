using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ProductRequests.Api.ExceptionHandling;

public sealed partial class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ExceptionDescriptor descriptor = ExceptionDescriptor.From(exception);
        if (descriptor.Status >= 500)
        {
            LogUnexpectedError(logger, httpContext.TraceIdentifier, exception);
        }
        else
        {
            LogKnownError(logger, descriptor.Code, httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = descriptor.Status;
        var problem = new ProblemDetails
        {
            Type = $"https://product-requests.local/errors/{descriptor.Code.ToLowerInvariant().Replace('_', '-')}",
            Title = descriptor.Title,
            Status = descriptor.Status,
            Detail = descriptor.Detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["code"] = descriptor.Code;
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    [LoggerMessage(1, LogLevel.Error, "Unexpected request failure. TraceId: {TraceId}")]
    private static partial void LogUnexpectedError(ILogger logger, string traceId, Exception exception);

    [LoggerMessage(2, LogLevel.Warning, "Request failed with code {Code}. TraceId: {TraceId}")]
    private static partial void LogKnownError(ILogger logger, string code, string traceId);
}
