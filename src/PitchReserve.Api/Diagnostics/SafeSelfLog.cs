namespace PitchReserve.Api.Diagnostics;

using Serilog.Debugging;

public static class SafeSelfLog
{
    private static long _lastLoggedTicks;
    private static readonly long MinIntervalTicks = TimeSpan.FromSeconds(5).Ticks;

    public static void Enable(TextWriter? output = null, TimeSpan? minInterval = null)
    {
        var targetOutput = output ?? Console.Error;
        var intervalTicks = minInterval?.Ticks ?? MinIntervalTicks;

        SelfLog.Enable(_ =>
        {
            var now = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read(ref _lastLoggedTicks);
            if (now - last >= intervalTicks)
            {
                if (Interlocked.CompareExchange(ref _lastLoggedTicks, now, last) == last)
                {
                    targetOutput.WriteLine("[PitchReserve-Internal] Serilog internal diagnostic emitted (payloads redacted).");
                }
            }
        });
    }

    public static void Disable()
    {
        SelfLog.Disable();
        Interlocked.Exchange(ref _lastLoggedTicks, 0);
    }
}
