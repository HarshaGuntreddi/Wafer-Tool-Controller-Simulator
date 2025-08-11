namespace ControllerService.Data;

public class Repository
{
    private readonly WaferDbContext _db;

    public Repository(WaferDbContext db) => _db = db;

    public async Task<Guid> CreateRunAsync(CancellationToken ct)
    {
        var run = new RunEntity { Id = Guid.NewGuid(), StartedAtUtc = DateTime.UtcNow };
        _db.Runs.Add(run);
        await _db.SaveChangesAsync(ct);
        return run.Id;
    }

    public async Task AppendTelemetryAsync(Guid runId, DateTime tsUtc, double temperatureC, double pressureHpa, CancellationToken ct)
    {
        var entity = new TelemetryEntity { RunId = runId, TsUtc = tsUtc, TemperatureC = temperatureC, PressureHpa = pressureHpa };
        _db.Telemetry.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task AppendEventAsync(Guid runId, DateTime tsUtc, string state, string? message, CancellationToken ct)
    {
        var entity = new EventEntity { RunId = runId, TsUtc = tsUtc, State = state, Message = message };
        _db.Events.Add(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task CloseRunAsync(Guid runId, string finalStatus, DateTime endedAtUtc, CancellationToken ct)
    {
        var run = await _db.Runs.FindAsync(new object?[] { runId }, ct);
        if (run != null)
        {
            run.EndedAtUtc = endedAtUtc;
            run.FinalStatus = finalStatus;
            await _db.SaveChangesAsync(ct);
        }
    }
}
