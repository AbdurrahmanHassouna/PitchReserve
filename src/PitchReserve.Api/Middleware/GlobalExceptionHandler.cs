namespace PitchReserve.Api.Middleware;

using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using Application.Common.Exceptions;
using Domain.Exceptions;
using Serilog.Context;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var correlationId = httpContext.Items[CorrelationContextMiddleware.CorrelationIdItemKey] as string;
        if (string.IsNullOrWhiteSpace(correlationId) || !Guid.TryParse(correlationId, out var parsedGuid))
        {
            correlationId = Guid.TryParse(httpContext.TraceIdentifier, out var parsedTraceId)
                ? parsedTraceId.ToString("D")
                : Guid.NewGuid().ToString("D");
        }
        else
        {
            correlationId = parsedGuid.ToString("D");
        }

        if (!httpContext.Response.HasStarted)
        {
            httpContext.Response.Headers[CorrelationContextMiddleware.CorrelationIdHeader] = correlationId;
        }

        var traceId = httpContext.Items[CorrelationContextMiddleware.TraceIdItemKey] as string;
        if (string.IsNullOrWhiteSpace(traceId))
        {
            var activity = Activity.Current;
            if (activity != null && activity.IdFormat == ActivityIdFormat.W3C && activity.TraceId != default)
            {
                traceId = activity.TraceId.ToString();
            }
        }

        var spanId = httpContext.Items[CorrelationContextMiddleware.SpanIdItemKey] as string;
        if (string.IsNullOrWhiteSpace(spanId))
        {
            var activity = Activity.Current;
            if (activity != null && activity.IdFormat == ActivityIdFormat.W3C && activity.SpanId != default)
            {
                spanId = activity.SpanId.ToString();
            }
        }

        string? userId = null;
        if (httpContext.User?.Identity?.IsAuthenticated == true)
        {
            userId = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? httpContext.User.FindFirst("sub")?.Value;
        }

        // Re-scope authenticated UserId, CorrelationId, and W3C TraceId/SpanId so they are present on GlobalExceptionHandler log records
        using var userScope = !string.IsNullOrEmpty(userId)
            ? LogContext.PushProperty("UserId", userId)
            : null;
        using var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);
        using var traceScope = !string.IsNullOrEmpty(traceId)
            ? LogContext.PushProperty("TraceId", traceId)
            : null;
        using var spanScope = !string.IsNullOrEmpty(spanId)
            ? LogContext.PushProperty("SpanId", spanId)
            : null;

        var problemDetails = new ProblemDetails
        {
            Instance = $"urn:uuid:{correlationId}"
        };

        problemDetails.Extensions["correlationId"] = correlationId;
        if (!string.IsNullOrEmpty(traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }

        var isBenignDisconnect = httpContext.RequestAborted.IsCancellationRequested &&
                                 (exception is OperationCanceledException or IOException);

        if (isBenignDisconnect)
        {
            _logger.LogInformation(
                "Request was aborted by client for {ExceptionType}",
                exception.GetType().Name);

            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.StatusCode = 499;
            }

            return true;
        }

        switch (exception)
        {
            case ValidationException validationException:
                _logger.LogInformation(
                    "Request validation failed with {ErrorCount} errors for {ExceptionType}",
                    validationException.Errors.Count,
                    validationException.GetType().Name);
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Validation error";
                problemDetails.Detail = validationException.Message;
                problemDetails.Extensions["errors"] = validationException.Errors;
                break;

            case NotFoundException notFoundException:
                _logger.LogInformation(
                    "Resource not found with {ExceptionType}",
                    notFoundException.GetType().Name);
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Resource not found";
                problemDetails.Detail = notFoundException.Message;
                break;

            case ConflictException conflictException:
                _logger.LogWarning(
                    "Conflict occurred during operation with {ExceptionType}",
                    conflictException.GetType().Name);
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Conflict";
                problemDetails.Detail = conflictException.Message;
                break;

            case SynchronousAccessException synchronousAccessException:
                _logger.LogWarning(
                    "Resource concurrency conflict with {ExceptionType}",
                    synchronousAccessException.GetType().Name);
                problemDetails.Status = StatusCodes.Status409Conflict;
                problemDetails.Title = "Resource is busy";
                problemDetails.Detail = synchronousAccessException.Message;
                break;

            case DomainException domainException:
                _logger.LogInformation(
                    "Domain rule violation with {ExceptionType}",
                    domainException.GetType().Name);
                problemDetails.Status = StatusCodes.Status400BadRequest;
                problemDetails.Title = "Domain rule violation";
                problemDetails.Detail = domainException.Message;
                break;

            case ForbiddenAccessException forbiddenAccessException:
                _logger.LogWarning(
                    "Forbidden access attempt with {ExceptionType}",
                    forbiddenAccessException.GetType().Name);
                problemDetails.Status = StatusCodes.Status403Forbidden;
                problemDetails.Title = "Forbidden";
                problemDetails.Detail = forbiddenAccessException.Message;
                break;

            case UnauthorizedAccessException unauthorizedAccessException:
                _logger.LogWarning(
                    "Unauthorized access attempt with {ExceptionType}",
                    unauthorizedAccessException.GetType().Name);
                problemDetails.Status = StatusCodes.Status401Unauthorized;
                problemDetails.Title = "Unauthorized";
                problemDetails.Detail = unauthorizedAccessException.Message;
                break;

            default:
                // Only unexpected 500 errors are logged at Error level.
                // Prevent exception message leakage: log TYPE + StackTrace, NOT raw Message or ToString().
                _logger.LogError(
                    "Unexpected exception of type {ExceptionType} occurred: {StackTrace}",
                    exception.GetType().FullName,
                    exception.StackTrace);
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "An unexpected error occurred";
                problemDetails.Detail = "An unexpected error occurred. Please contact support.";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;

        // If request was aborted by client, do not attempt writing response body to closed connection
        if (httpContext.RequestAborted.IsCancellationRequested)
        {
            return true;
        }

        await httpContext.Response.WriteAsJsonAsync(problemDetails,
            options: (System.Text.Json.JsonSerializerOptions?)null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);

        return true;
    }
}
