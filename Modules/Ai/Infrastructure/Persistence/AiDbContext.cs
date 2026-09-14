using FoodDiary.Modules.Ai.Domain.Entities;
using FoodDiary.Modules.Ai.PersistenceModel;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace FoodDiary.Modules.Ai.Infrastructure.Persistence;

public sealed class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options) {
    public DbSet<AiUsage> AiUsages => Set<AiUsage>();
    public DbSet<AiPromptTemplate> AiPromptTemplates => Set<AiPromptTemplate>();
    internal DbSet<AiQuotaPeriod> AiQuotaPeriods => Set<AiQuotaPeriod>();
    internal DbSet<AiQuotaReservation> AiQuotaReservations => Set<AiQuotaReservation>();
    internal DbSet<FoodRecognitionJob> FoodRecognitionJobs => Set<FoodRecognitionJob>();

    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default) {
        try {
            return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken).ConfigureAwait(false);
        } catch (DbUpdateException exception) when (exception.InnerException is PostgresException {
            SqlState: PostgresErrorCodes.UniqueViolation,
            SchemaName: "public",
            TableName: "AiPromptTemplates",
            ConstraintName: "IX_AiPromptTemplates_Key_Locale",
        }) {
            // The caller's unit of work rolls back and disposes the failed scope; do not retry tracked inserts.
            throw new DbUpdateConcurrencyException("An AI prompt with this key and locale was created by another request.", exception);
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyAiPersistenceModel();
}
