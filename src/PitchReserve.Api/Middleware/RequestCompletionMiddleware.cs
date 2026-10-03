namespace PitchReserve.Api.Middleware;

using System.Diagnostics;
using System.IO;
using System.Security.Claims;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using PitchReserve.Api.Options;
using Serilog.Context;

public class RequestCompletionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestCompletionMiddleware> _logger;
    private readonly int _slowRequestThresholdMs;

    public RequestCompletionMiddleware(
        RequestDelegate next,
        ILogger<RequestCompletionMiddleware> logger,
        IOptions<RequestLoggingOptions> options)
    {
        _next = next;
        _logger = logger;
        _slowRequestThresholdMs = options.Value.SlowRequestThresholdMs > 0
            ? options.Value.SlowRequestThresholdMs
            : 500;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        Exception? escapedException = null;

        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            escapedException = ex;
            throw;
        }
        finally
        {
            stopwatch.Stop();
            LogRequestCompletion(context, stopwatch.Elapsed.TotalMilliseconds, escapedException);
        }
    }

    private void LogRequestCompletion(HttpContext context, double elapsedMs, Exception? escapedException)
    {
        var method = context.Request.Method;
        var routeTemplate = ResolveRouteTemplate(context);
        var statusCode = context.Response.StatusCode;
        var isClientAborted = context.RequestAborted.IsCancellationRequested;

        var error = escapedException ?? context.Features.Get<IExceptionHandlerFeature>()?.Error;

        string outcome;
        if (error is OperationCanceledException)
        {
            outcome = "Cancelled";
        }
        else if (error is IOException)
        {
            outcome = isClientAborted ? "Cancelled" : "Failed";
        }
        else if (error != null)
        {
            outcome = "Failed";
        }
        else if (isClientAborted)
        {
            outcome = "Cancelled";
        }
        else if (statusCode >= StatusCodes.Status400BadRequest)
        {
            outcome = "Failed";
        }
        else
        {
            outcome = "Success";
        }

        // Diagnostic middleware: do NOT mutate HTTP Response.StatusCode.
        // If response has already started, retain actual sent status and record outcome metadata.
        // If response has not started, record effective expected status for escaped/server failures or benign client aborts.
        if (!context.Response.HasStarted)
        {
            var isBenignClientAbort = isClientAborted && (error is null or OperationCanceledException or IOException);
            if (isBenignClientAbort)
            {
                if (statusCode < StatusCodes.Status400BadRequest)
                {
                    statusCode = 499; // Client Closed Request
                }
            }
            else if (outcome == "Failed" || (error is OperationCanceledException && !isClientAborted))
            {
                if (statusCode < StatusCodes.Status400BadRequest)
                {
                    statusCode = StatusCodes.Status500InternalServerError;
                }
            }
        }

        var userId = ResolveUserId(context);
        using var userScope = !string.IsNullOrEmpty(userId)
            ? LogContext.PushProperty("UserId", userId)
            : null;
        using var outcomeScope = LogContext.PushProperty("Outcome", outcome);

        var useAbortedTemplate = isClientAborted && outcome == "Cancelled";

        if (useAbortedTemplate)
        {
            if (elapsedMs >= _slowRequestThresholdMs)
            {
                _logger.LogWarning(
                    "HTTP {Method} {RouteTemplate} was aborted after {ElapsedMs:0.0000} ms with status {StatusCode} (slow)",
                    method,
                    routeTemplate,
                    elapsedMs,
                    statusCode);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {Method} {RouteTemplate} was aborted after {ElapsedMs:0.0000} ms with status {StatusCode}",
                    method,
                    routeTemplate,
                    elapsedMs,
                    statusCode);
            }
        }
        else if (elapsedMs >= _slowRequestThresholdMs)
        {
            _logger.LogWarning(
                "HTTP {Method} {RouteTemplate} responded {StatusCode} in {ElapsedMs:0.0000} ms (slow)",
                method,
                routeTemplate,
                statusCode,
                elapsedMs);
        }
        else
        {
            _logger.LogInformation(
                "HTTP {Method} {RouteTemplate} responded {StatusCode} in {ElapsedMs:0.0000} ms",
                method,
                routeTemplate,
                statusCode,
                elapsedMs);
        }
    }

    private static string ResolveRouteTemplate(HttpContext context)
    {
        var endpoint = context.GetEndpoint() as RouteEndpoint
            ?? context.Features.Get<IExceptionHandlerPathFeature>()?.Endpoint as RouteEndpoint
            ?? context.Features.Get<IExceptionHandlerFeature>()?.Endpoint as RouteEndpoint;

        if (endpoint?.RoutePattern.RawText is { } rawPattern && !string.IsNullOrWhiteSpace(rawPattern))
        {
            return rawPattern.StartsWith('/') ? rawPattern : "/" + rawPattern;
        }

        return "{unmatched}";
    }

    private static string? ResolveUserId(HttpContext context)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                ?? user.FindFirst("sub")?.Value;
        }

        return null;
    }
}
