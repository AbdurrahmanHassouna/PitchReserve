namespace PitchReserve.Api.Diagnostics;

using Microsoft.Extensions.Hosting;

public class LogDropNotifierService : BackgroundService
{
    private readonly ILogDropMonitor _dropMonitor;
    private readonly TimeSpan _checkInterval;
    private long _lastReportedCount;

    private readonly TextWriter _output;

    public LogDropNotifierService(ILogDropMonitor dropMonitor)
        : this(dropMonitor, TimeSpan.FromSeconds(30), null)
    {
    }

    public LogDropNotifierService(ILogDropMonitor dropMonitor, TimeSpan checkInterval)
        : this(dropMonitor, checkInterval, null)
    {
    }

    public LogDropNotifierService(ILogDropMonitor dropMonitor, TimeSpan checkInterval, TextWriter? output)
    {
        _dropMonitor = dropMonitor;
        _checkInterval = checkInterval;
        _output = output ?? Console.Error;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_checkInterval);

        try
        {
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                ReportDropsIfAny();
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Expected host shutdown
        }
        finally
        {
            // Report any remaining unnotified drops on shutdown
            ReportDropsIfAny();
        }
    }

    private void ReportDropsIfAny()
    {
        var dropped = _dropMonitor.DroppedMessagesCount;
        if (dropped > _lastReportedCount)
        {
            var diff = dropped - _lastReportedCount;
            _lastReportedCount = dropped;
            _output.WriteLine($"[PitchReserve-DropMonitor] Cumulative dropped log events: {dropped} (+{diff}).");
        }
    }
}
