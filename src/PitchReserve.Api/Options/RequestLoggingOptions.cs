namespace PitchReserve.Api.Options;

using System.ComponentModel.DataAnnotations;

public class RequestLoggingOptions
{
    public const string SectionName = "DiagnosticLogging";

    [Range(1, int.MaxValue, ErrorMessage = "SlowRequestThresholdMs must be greater than zero.")]
    public int SlowRequestThresholdMs { get; set; } = 500;

    [Required(AllowEmptyStrings = false, ErrorMessage = "LogDirectory is required and cannot be empty.")]
    public string LogDirectory { get; set; } = "logs";

    [Range(1024 * 1024, 1024 * 1024 * 1024, ErrorMessage = "FileSizeBytes must be between 1MiB and 1GiB.")]
    public long FileSizeBytes { get; set; } = 52_428_800; // 50 MiB

    [Range(1, 365, ErrorMessage = "RetainedFileDays must be between 1 and 365.")]
    public int RetainedFileDays { get; set; } = 31;

    [Range(1, 1000, ErrorMessage = "RetainedFileCountLimit must be between 1 and 1000.")]
    public int RetainedFileCountLimit { get; set; } = 31;

    [Range(100, 1_000_000, ErrorMessage = "AsyncBufferSize must be between 100 and 1,000,000.")]
    public int AsyncBufferSize { get; set; } = 10000;
}
