using Shared.Domain;
using Shared.Protocol;

namespace ControllerService.Controller;

public class TelemetryIngestor
{
    private readonly ControllerState _state;

    public TelemetryIngestor(ControllerState state) => _state = state;

    public void IngestTelemetry(Telemetry telemetry) => _state.AddTelemetry(telemetry);

    public void IngestEvent(Event evt)
    {
        if (evt.Name == "state" && evt.Data != null && Enum.TryParse<ToolState>(evt.Data, out var state))
            _state.UpdateState(state);
    }
}
