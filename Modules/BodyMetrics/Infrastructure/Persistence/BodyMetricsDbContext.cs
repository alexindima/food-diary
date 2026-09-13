using FoodDiary.Domain.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;

public sealed class BodyMetricsDbContext(DbContextOptions<BodyMetricsDbContext> options) : DbContext(options) {
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<WaistEntry> WaistEntries => Set<WaistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyBodyMetricsPersistenceModel();
    }
}
