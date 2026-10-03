namespace PitchReserve.Application.Common.Behaviors;

using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PitchReserve.Application.Common.Options;

public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;
    private readonly int _slowThresholdMs;

    public LoggingBehavior(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        IOptions<ApplicationLoggingOptions>? options = null)
    {
        _logger = logger;
        _slowThresholdMs = options?.Value?.SlowHandlerThresholdMs > 0
            ? options.Value.SlowHandlerThresholdMs
            : 500;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var stopwatch = Stopwatch.StartNew();
        var outcome = "Success";

        try
        {
            return await next();
        }
        catch (OperationCanceledException)
        {
            outcome = "Cancelled";
            // Do not duplicate exception logging here; GlobalExceptionHandler owns error details.
            throw;
        }
        catch (Exception)
        {
            outcome = "Failed";
            // Do not duplicate exception logging here; GlobalExceptionHandler owns error details.
            throw;
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;

            if (elapsedMs >= _slowThresholdMs)
            {
                _logger.LogWarning(
                    "Execution of {RequestName} took {ElapsedMs:0.0000} ms (slow, outcome: {Outcome})",
                    requestName,
                    elapsedMs,
                    outcome);
            }

            if (_logger.IsEnabled(LogLevel.Debug))
            {
                _logger.LogDebug(
                    "Handled {RequestName} in {ElapsedMs:0.0000} ms with outcome {Outcome}",
                    requestName,
                    elapsedMs,
                    outcome);
            }
        }
    }
}
