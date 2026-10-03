namespace PitchReserve.Api.Diagnostics;

using Serilog.Sinks.Async;

public interface ILogDropMonitor
{
    long DroppedMessagesCount { get; }
    int CurrentQueueCount { get; }
    int BufferSize { get; }
}

public class LogDropMonitor : IAsyncLogEventSinkMonitor, ILogDropMonitor
{
    private readonly object _lock = new();
    private IAsyncLogEventSinkInspector? _inspector;
    private long _cumulativeDroppedMessagesCount;
    private int _lastBufferSize;

    public void StartMonitoring(IAsyncLogEventSinkInspector inspector)
    {
        lock (_lock)
        {
            _inspector = inspector;
            _lastBufferSize = inspector.BufferSize;
        }
    }

    public void StopMonitoring(IAsyncLogEventSinkInspector inspector)
    {
        lock (_lock)
        {
            if (ReferenceEquals(_inspector, inspector))
            {
                _cumulativeDroppedMessagesCount += inspector.DroppedMessagesCount;
                _lastBufferSize = inspector.BufferSize;
                _inspector = null;
            }
        }
    }

    public long DroppedMessagesCount
    {
        get
        {
            lock (_lock)
            {
                return _cumulativeDroppedMessagesCount + (_inspector?.DroppedMessagesCount ?? 0);
            }
        }
    }

    public int CurrentQueueCount
    {
        get
        {
            lock (_lock)
            {
                return _inspector?.Count ?? 0;
            }
        }
    }

    public int BufferSize
    {
        get
        {
            lock (_lock)
            {
                return _inspector?.BufferSize ?? _lastBufferSize;
            }
        }
    }
}
