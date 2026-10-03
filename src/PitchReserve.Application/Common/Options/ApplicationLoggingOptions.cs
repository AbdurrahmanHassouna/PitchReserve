namespace PitchReserve.Application.Common.Options;

using System.ComponentModel.DataAnnotations;

public class ApplicationLoggingOptions
{
    public const string SectionName = "ApplicationLogging";

    [Range(1, int.MaxValue, ErrorMessage = "SlowHandlerThresholdMs must be greater than zero.")]
    public int SlowHandlerThresholdMs { get; set; } = 500;
}
