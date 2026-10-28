using System.IO;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using PitchReserve.Application.Common.Exceptions;
using PitchReserve.Domain.Exceptions;

namespace PitchReserve.Api.Middleware;

public sealed class GlobalExceptionHandler : IExceptionHandler
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
        if (httpContext.Response.HasStarted)
            return false;

        var traceId = httpContext.TraceIdentifier;

        if (httpContext.RequestAborted.IsCancellationRequested &&
            exception is (OperationCanceledException or IOException))
        {
            _logger.LogInformation("Request {TraceId} was cancelled by the client.", traceId);
            httpContext.Response.StatusCode = 499;
            return true;
        }

        var status = StatusCodes.Status500InternalServerError;
        var title = "An unexpected error occurred";
        var detail = "An unexpected error occurred. Please contact support.";

        switch (exception)
        {
            case ValidationException validationException:
                status = StatusCodes.Status400BadRequest;
                title = "Validation error";
                detail = validationException.Message;
                break;
            case NotFoundException notFoundException:
                status = StatusCodes.Status404NotFound;
                title = "Resource not found";
                detail = notFoundException.Message;
                break;
            case ConflictException conflictException:
                status = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = conflictException.Message;
                break;
            case SynchronousAccessException synchronousAccessException:
                status = StatusCodes.Status409Conflict;
                title = "Resource is busy";
                detail = synchronousAccessException.Message;
                break;
            case DbUpdateConcurrencyException:
                status = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = "The resource changed. Refresh and try again.";
                break;
            case DbUpdateException updateException when updateException.InnerException is SqlException { Number: 2601 or 2627 }:
                status = StatusCodes.Status409Conflict;
                title = "Conflict";
                detail = "This record already exists.";
                break;
            case DomainException domainException:
                status = StatusCodes.Status400BadRequest;
                title = "Domain rule violation";
                detail = domainException.Message;
                break;
            case ForbiddenAccessException forbiddenException:
                status = StatusCodes.Status403Forbidden;
                title = "Forbidden";
                detail = forbiddenException.Message;
                break;
            case UnauthorizedAccessException unauthorizedException:
                status = StatusCodes.Status401Unauthorized;
                title = "Unauthorized";
                detail = unauthorizedException.Message;
                break;
        }

        if (status >= StatusCodes.Status500InternalServerError)
        {
            _logger.LogError("Unhandled {ExceptionType} for request {TraceId}: {StackTrace}",
                exception.GetType().FullName, traceId, exception.StackTrace);
        }
        else
        {
            _logger.LogInformation("Request {TraceId} returned HTTP {StatusCode} ({ExceptionType}).",
                traceId, status, exception.GetType().Name);
        }

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
        };
        problem.Extensions["traceId"] = traceId;
        if (exception is ValidationException validation)
            problem.Extensions["errors"] = validation.Errors;

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(value: problem,
            options:null,
            contentType: "application/problem+json",
            cancellationToken: cancellationToken);
        return true;
    }
}
