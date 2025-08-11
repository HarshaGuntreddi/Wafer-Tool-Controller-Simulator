using Microsoft.EntityFrameworkCore;

namespace ControllerService.Data;

public class WaferDbContext : DbContext
{
    public WaferDbContext(DbContextOptions<WaferDbContext> options) : base(options) { }

    public DbSet<RunEntity> Runs => Set<RunEntity>();
    public DbSet<TelemetryEntity> Telemetry => Set<TelemetryEntity>();
    public DbSet<EventEntity> Events => Set<EventEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TelemetryEntity>()
            .HasIndex(t => new { t.RunId, t.TsUtc });
        modelBuilder.Entity<EventEntity>()
            .HasIndex(e => new { e.RunId, e.TsUtc });
    }
}
