using FoodDiary.Domain.Entities.Ai;
using FoodDiary.Infrastructure.Persistence.Ai;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed class AiDbContext(DbContextOptions<AiDbContext> options) : DbContext(options) {
    public DbSet<AiUsage> AiUsages => Set<AiUsage>();
    public DbSet<AiPromptTemplate> AiPromptTemplates => Set<AiPromptTemplate>();
    internal DbSet<AiQuotaPeriod> AiQuotaPeriods => Set<AiQuotaPeriod>();
    internal DbSet<AiQuotaReservation> AiQuotaReservations => Set<AiQuotaReservation>();
    internal DbSet<FoodRecognitionJob> FoodRecognitionJobs => Set<FoodRecognitionJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ApplyAiPersistenceModel();
}
