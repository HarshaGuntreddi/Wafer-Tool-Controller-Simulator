namespace ControllerService.Data;

public class RunEntity
{
    public Guid Id { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? EndedAtUtc { get; set; }
    public string? FinalStatus { get; set; }
    public ICollection<TelemetryEntity> Telemetry { get; set; } = new List<TelemetryEntity>();
    public ICollection<EventEntity> Events { get; set; } = new List<EventEntity>();
}

public class TelemetryEntity
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public RunEntity Run { get; set; } = null!;
    public DateTime TsUtc { get; set; }
    public double TemperatureC { get; set; }
    public double PressureHpa { get; set; }
}

public class EventEntity
{
    public long Id { get; set; }
    public Guid RunId { get; set; }
    public RunEntity Run { get; set; } = null!;
    public DateTime TsUtc { get; set; }
    public string State { get; set; } = string.Empty;
    public string? Message { get; set; }
}
