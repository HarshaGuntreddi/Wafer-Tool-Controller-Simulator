namespace Shared.Protocol;

public record Envelope(Guid Id, string Type, DateTime Ts);

public enum Command
{
    HELLO,
    STATUS,
    LOAD,
    START,
    STOP,
    UNLOAD,
    SHUTDOWN
}

public record Ack(Guid Id);

public record Event(string Name, string? Data);

public record Telemetry(string Metric, double Value);

public record Error(string Code, string Message);

public record Heartbeat(DateTime Ts);
