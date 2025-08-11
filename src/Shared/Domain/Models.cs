namespace Shared.Domain;

public class Run
{
    public Guid Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class Telemetry
{
    public DateTime Timestamp { get; set; }
    public string Key { get; set; } = string.Empty;
    public double Value { get; set; }
}

public class Event
{
    public DateTime Timestamp { get; set; }
    public string Type { get; set; } = string.Empty;
    public string? Detail { get; set; }
}
