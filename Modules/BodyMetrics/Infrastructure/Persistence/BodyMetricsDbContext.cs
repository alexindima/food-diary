using Npgsql;
using FoodDiary.Modules.BodyMetrics.PersistenceModel;
using FoodDiary.Modules.BodyMetrics.Domain.Entities.Tracking;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.BodyMetrics.Infrastructure.Persistence;

public sealed class BodyMetricsDbContext(DbContextOptions<BodyMetricsDbContext> options) : DbContext(options) {
    public DbSet<WeightEntry> WeightEntries => Set<WeightEntry>();
    public DbSet<WaistEntry> WaistEntries => Set<WaistEntry>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
        try {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
        } catch (DbUpdateException exception) when (exception.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            SchemaName: "public",
        } postgres && (postgres is { TableName: "WeightEntries", ConstraintName: "IX_WeightEntries_UserId_Date" }
            or { TableName: "WaistEntries", ConstraintName: "IX_WaistEntries_UserId_Date" })) {
            // The owning unit of work rolls back; never retry or commit the failed tracked changes here.
            throw new DbUpdateConcurrencyException("A measurement for this date was saved by another request.", exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyBodyMetricsPersistenceModel();
    }
}
