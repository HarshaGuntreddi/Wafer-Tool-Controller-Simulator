using Shared.Domain;
using Shared.Protocol;
using Shared.Util;

namespace ToolSimulator.Simulator;

public class ToolCore
{
    private readonly StateMachine _state = new();
    private readonly FaultInjection _fault;

    public ToolCore(FaultInjection fault) => _fault = fault;

    public async Task HandleAsync(Envelope envelope, NetworkStream stream, CancellationToken token)
    {
        if (!Enum.TryParse<Command>(envelope.Type, out var cmd))
        {
            await SendAsync(stream, new Error("UNKNOWN", envelope.Type), token);
            return;
        }

        switch (cmd)
        {
            case Command.LOAD:
                await TransitionAsync(stream, envelope.Id, ToolState.LOADED, TimeSpan.FromSeconds(1), token);
                break;
            case Command.START:
                await TransitionAsync(stream, envelope.Id, ToolState.PROCESSING, TimeSpan.FromSeconds(2), token);
                _ = EmitTelemetry(stream, token);
                break;
            case Command.STOP:
                await TransitionAsync(stream, envelope.Id, ToolState.UNLOADING, TimeSpan.FromSeconds(1), token);
                break;
            case Command.UNLOAD:
                await TransitionAsync(stream, envelope.Id, ToolState.IDLE, TimeSpan.FromSeconds(1), token);
                break;
            case Command.STATUS:
                await SendAsync(stream, new Event("state", _state.State.ToString()), token);
                break;
            default:
                await SendAsync(stream, new Error("UNSUPPORTED", cmd.ToString()), token);
                break;
        }
    }

    private async Task TransitionAsync(NetworkStream stream, Guid id, ToolState target, TimeSpan delay, CancellationToken token)
    {
        try
        {
            _state.Validate(target);
            await Task.Delay(delay, token);
            _state.TryTransition(target);
            await SendAsync(stream, new Ack(id), token);
        }
        catch (Exception ex)
        {
            await SendAsync(stream, new Error("STATE", ex.Message), token);
        }
    }

    private async Task EmitTelemetry(NetworkStream stream, CancellationToken token)
    {
        while (_state.State == ToolState.PROCESSING && !token.IsCancellationRequested)
        {
            var telemetry = new Telemetry("metric", Random.Shared.NextDouble());
            await SendAsync(stream, telemetry, token);
            await Task.Delay(500, token);
        }
    }

    private async Task SendAsync<T>(NetworkStream stream, T message, CancellationToken token)
    {
        if (await _fault.ApplyAsync(stream, token))
            await Framing.WriteAsync(stream, message, Json.Options, token);
    }
}
