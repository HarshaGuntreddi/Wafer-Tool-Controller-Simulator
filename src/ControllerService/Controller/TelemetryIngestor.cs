using Shared.Domain;
using Shared.Protocol;
using ControllerService.Data;

namespace ControllerService.Controller;

public class TelemetryIngestor
{
    private readonly ControllerState _state;
    private readonly Repository _repository;
    private Guid? _currentRunId;

    public TelemetryIngestor(ControllerState state, Repository repository)
    {
        _state = state;
        _repository = repository;
    }

    public Guid? CurrentRunId => _currentRunId;

    public void BeginRun(Guid runId) => _currentRunId = runId;

    public void EndRun() => _currentRunId = null;

    public void IngestTelemetry(Telemetry telemetry)
    {
        _state.AddTelemetry(telemetry);
        var runId = _currentRunId;
        if (runId.HasValue)
        {
            double temp = 0, pressure = 0;
            if (telemetry.Metric.Equals("temperatureC", StringComparison.OrdinalIgnoreCase))
                temp = telemetry.Value;
            else if (telemetry.Metric.Equals("pressureHpa", StringComparison.OrdinalIgnoreCase))
                pressure = telemetry.Value;
            else
                return;

            _ = _repository.AppendTelemetryAsync(runId.Value, DateTime.UtcNow, temp, pressure, CancellationToken.None);
        }
    }

    public void IngestEvent(Event evt)
    {
        if (evt.Name == "state" && evt.Data != null && Enum.TryParse<ToolState>(evt.Data, out var state))
            _state.UpdateState(state);

        var runId = _currentRunId;
        if (runId.HasValue)
        {
            _ = _repository.AppendEventAsync(runId.Value, DateTime.UtcNow, evt.Name, evt.Data, CancellationToken.None);
        }
    }
}
