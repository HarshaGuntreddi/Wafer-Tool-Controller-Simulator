using System.Collections.Concurrent;
using Shared.Domain;
using Shared.Util;
using System.Text.Json;
using System.Threading.Channels;

namespace ControllerService.Controller;

public class ControllerState
{
    private readonly object _lock = new();
    private readonly int _telemetryCapacity = 1000;
    private readonly List<Shared.Domain.Telemetry> _telemetry = new();
    private readonly Channel<string> _stream = Channel.CreateUnbounded<string>();

    public bool Connected { get; private set; }
    public DateTime? LastHeartbeatUtc { get; private set; }
    public ToolState CurrentState { get; private set; } = ToolState.IDLE;

    public IReadOnlyList<Shared.Domain.Telemetry> GetTelemetryLatest(int limit)
    {
        lock (_lock)
        {
            var count = Math.Min(limit, _telemetry.Count);
            return _telemetry.Skip(_telemetry.Count - count).ToList();
        }
    }

    public IAsyncEnumerable<string> StreamAsync(CancellationToken cancellationToken) => _stream.Reader.ReadAllAsync(cancellationToken);

    public void UpdateConnection(bool connected)
    {
        lock (_lock)
        {
            Connected = connected;
        }
        PublishStatus();
    }

    public void UpdateHeartbeat(DateTime utc)
    {
        lock (_lock)
        {
            LastHeartbeatUtc = utc;
        }
        PublishStatus();
    }

    public void UpdateState(ToolState state)
    {
        lock (_lock)
        {
            CurrentState = state;
        }
        PublishStatus();
    }

    public void AddTelemetry(Shared.Domain.Telemetry telemetry)
    {
        lock (_lock)
        {
            if (_telemetry.Count >= _telemetryCapacity)
                _telemetry.RemoveAt(0);
            _telemetry.Add(telemetry);
        }
        PublishTelemetry(telemetry);
    }

    private void PublishStatus()
    {
        var json = JsonSerializer.Serialize(new { type = "status", connected = Connected, lastHeartbeatUtc = LastHeartbeatUtc, state = CurrentState }, Json.Options);
        _stream.Writer.TryWrite(json);
    }

    private void PublishTelemetry(Shared.Domain.Telemetry telemetry)
    {
        var json = JsonSerializer.Serialize(new { type = "telemetry", telemetry }, Json.Options);
        _stream.Writer.TryWrite(json);
    }
}
