using FoodDiary.Modules.ContentReports.Domain.Entities;
using FoodDiary.Modules.ContentReports.PersistenceModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Modules.ContentReports.Infrastructure.Persistence;

public sealed class ContentReportsDbContext(DbContextOptions<ContentReportsDbContext> options) : DbContext(options) {
    public DbSet<ContentReport> ContentReports => Set<ContentReport>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
        try {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
        } catch (DbUpdateException exception) when (exception.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            SchemaName: "public",
            TableName: "ContentReports",
            ConstraintName: "IX_ContentReports_UserId_TargetType_TargetId",
        }) {
            // The caller's unit of work owns rollback; do not retry or commit failed changes here.
            throw new DbUpdateConcurrencyException("This content was already reported by another request.", exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyContentReportsPersistenceModel();
    }
}
