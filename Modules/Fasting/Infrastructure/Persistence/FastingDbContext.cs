using FoodDiary.Domain.Entities.Tracking.Fasting;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Fasting.Infrastructure.Persistence;

public sealed class FastingDbContext(DbContextOptions<FastingDbContext> options) : DbContext(options) {
    public DbSet<FastingPlan> FastingPlans => Set<FastingPlan>();
    public DbSet<FastingOccurrence> FastingOccurrences => Set<FastingOccurrence>();
    public DbSet<FastingCheckIn> FastingCheckIns => Set<FastingCheckIn>();
    public DbSet<FastingSession> FastingSessions => Set<FastingSession>();
    public DbSet<FastingTelemetryEvent> FastingTelemetryEvents => Set<FastingTelemetryEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyFastingPersistenceModel();
    }
}
