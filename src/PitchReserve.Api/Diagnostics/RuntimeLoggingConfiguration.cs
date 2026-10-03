namespace PitchReserve.Api.Diagnostics;

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using PitchReserve.Api.Options;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.Async;

public static class RuntimeLoggingConfiguration
{
    public const string DefaultClefFileName = "pitchreserve-.clef";

    public static LoggerConfiguration ConfigurePitchReserveLogging(
        this LoggerConfiguration configuration,
        IConfiguration appConfiguration,
        IAsyncLogEventSinkMonitor? dropMonitor = null,
        string? baseDirectory = null,
        Action<LoggerSinkConfiguration>? configureSinks = null)
    {
        var loggingOptions = appConfiguration
            .GetSection(RequestLoggingOptions.SectionName)
            .Get<RequestLoggingOptions>() ?? new RequestLoggingOptions();

        // Validate options before sink instantiation or directory creation
        Validator.ValidateObject(loggingOptions, new ValidationContext(loggingOptions), validateAllProperties: true);

        var logDirectory = loggingOptions.LogDirectory;
        if (!Path.IsPathRooted(logDirectory))
        {
            logDirectory = Path.Combine(baseDirectory ?? AppContext.BaseDirectory, logDirectory);
        }
        Directory.CreateDirectory(logDirectory);

        var logFilePath = Path.Combine(logDirectory, DefaultClefFileName);

        // Apply defaults BEFORE ReadFrom.Configuration so configuration overrides take effect
        configuration
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .ReadFrom.Configuration(appConfiguration)
            .Enrich.FromLogContext()
            .ApplyPitchReservePrivacyPolicies()
            .WriteTo.Async(
                sinkConfig =>
                {
                    if (configureSinks != null)
                    {
                        configureSinks(sinkConfig);
                    }
                    else
                    {
                        sinkConfig.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}");
                        sinkConfig.File(
                            formatter: new CompactJsonFormatter(),
                            path: logFilePath,
                            rollingInterval: RollingInterval.Day,
                            fileSizeLimitBytes: loggingOptions.FileSizeBytes,
                            rollOnFileSizeLimit: true,
                            retainedFileTimeLimit: TimeSpan.FromDays(loggingOptions.RetainedFileDays),
                            retainedFileCountLimit: loggingOptions.RetainedFileCountLimit);
                    }
                },
                bufferSize: loggingOptions.AsyncBufferSize,
                blockWhenFull: false,
                monitor: dropMonitor);

        return configuration;
    }
}
