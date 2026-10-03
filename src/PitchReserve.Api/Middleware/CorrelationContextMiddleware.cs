namespace PitchReserve.Api.Middleware;

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

public class CorrelationContextMiddleware
{
    public const string CorrelationIdHeader = "X-Correlation-Id";
    public const string CorrelationIdItemKey = "CorrelationId";
    public const string TraceIdItemKey = "TraceId";
    public const string SpanIdItemKey = "SpanId";

    private readonly RequestDelegate _next;

    public CorrelationContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = ResolveCorrelationId(context);

        context.Items[CorrelationIdItemKey] = correlationId;
        context.TraceIdentifier = correlationId;

        // Set correlation header upfront if response has not started
        if (!context.Response.HasStarted)
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
        }

        // Echo resolved correlationId unconditionally in OnStarting
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        // Only set TraceId and SpanId if an actual valid W3C Activity exists
        var activity = Activity.Current;
        var hasValidW3CActivity = activity != null &&
                                  activity.IdFormat == ActivityIdFormat.W3C &&
                                  activity.TraceId != default;

        var traceId = hasValidW3CActivity
            ? activity!.TraceId.ToString()
            : null;

        var spanId = hasValidW3CActivity && activity!.SpanId != default
            ? activity.SpanId.ToString()
            : null;

        if (!string.IsNullOrEmpty(traceId))
        {
            context.Items[TraceIdItemKey] = traceId;
        }

        if (!string.IsNullOrEmpty(spanId))
        {
            context.Items[SpanIdItemKey] = spanId;
        }

        using var correlationScope = LogContext.PushProperty("CorrelationId", correlationId);
        using var traceScope = !string.IsNullOrEmpty(traceId) ? LogContext.PushProperty("TraceId", traceId) : null;
        using var spanScope = !string.IsNullOrEmpty(spanId) ? LogContext.PushProperty("SpanId", spanId) : null;

        await _next(context);
    }

    private static string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(CorrelationIdHeader, out var values))
        {
            // Reject multiple headers
            if (values.Count == 1)
            {
                var candidate = values[0];
                // Reject oversized or empty
                if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= 64)
                {
                    // Reject non-UUID / malformed
                    if (Guid.TryParse(candidate.Trim(), out var parsedGuid))
                    {
                        return parsedGuid.ToString("D");
                    }
                }
            }
        }

        return Guid.NewGuid().ToString("D");
    }
}
