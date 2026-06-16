using ITS.Application.Common.Exceptions;
using ITS.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ITS.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext context,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException ex => (StatusCodes.Status404NotFound, "Resource Not Found", ex.Message),
            ForbiddenAccessException ex => (StatusCodes.Status403Forbidden, "Forbidden", ex.Message),
            ConflictException ex => (StatusCodes.Status409Conflict, "Concurrency Conflict", ex.Message),
            Application.Common.Exceptions.ValidationException ex => HandleValidation(context, ex, cancellationToken),
            DomainException ex => (StatusCodes.Status422UnprocessableEntity, "Business Rule Violation", ex.Message),
            WorkflowTransitionException ex => (StatusCodes.Status422UnprocessableEntity, "Workflow Transition Error", ex.Message),
            UnauthorizedAccessException ex => (StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", null)
        };

        if (exception is Application.Common.Exceptions.ValidationException)
            return true; // Already handled inline

        if (statusCode == StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception on {Method} {Path}", context.Request.Method, context.Request.Path);

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        };

        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }

    private static (int, string, string?) HandleValidation(
        HttpContext context,
        Application.Common.Exceptions.ValidationException ex,
        CancellationToken cancellationToken)
    {
        // We handle this separately as a 400 ValidationProblemDetails
        var problemDetails = new ValidationProblemDetails(ex.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation Failed",
            Instance = context.Request.Path,
            Extensions = { ["traceId"] = context.TraceIdentifier }
        };

        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.WriteAsJsonAsync(problemDetails, cancellationToken).GetAwaiter().GetResult();

        return (0, "", null); // Signal handled
    }
}
