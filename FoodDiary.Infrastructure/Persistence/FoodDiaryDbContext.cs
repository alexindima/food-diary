using Microsoft.EntityFrameworkCore;
using FoodDiary.Infrastructure.Persistence.Audit;
using FoodDiary.Modules.Fasting.Infrastructure.Persistence;
using FoodDiary.Modules.Hydration.Infrastructure.Persistence;
using FoodDiary.Modules.DailyAdvices.Infrastructure.Persistence;
using FoodDiary.Modules.Lessons.Infrastructure.Persistence;
using FoodDiary.Modules.ContentReports.Infrastructure.Persistence;
using FoodDiary.Modules.Favorites.Infrastructure.Persistence;
using FoodDiary.Modules.WeeklyGoals.Infrastructure.Persistence;
using FoodDiary.Modules.Cycles.Infrastructure.Persistence;
using FoodDiary.Modules.OpenFoodFacts.Infrastructure.Model;
using FoodDiary.Modules.Marketing.Infrastructure.Persistence;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext(DbContextOptions<FoodDiaryDbContext> options) : DbContext(options) {
    internal DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FoodDiaryDbContext).Assembly);
        modelBuilder.ApplyFastingPersistenceModel();
        modelBuilder.ApplyHydrationPersistenceModel();
        modelBuilder.ApplyDailyAdvicesPersistenceModel();
        modelBuilder.ApplyLessonsPersistenceModel();
        modelBuilder.ApplyContentReportsPersistenceModel();
        modelBuilder.ApplyFavoritesPersistenceModel();
        modelBuilder.ApplyWeeklyGoalsPersistenceModel();
        modelBuilder.ApplyGamificationPersistenceModel();
        modelBuilder.ApplyImagesPersistenceModel();
        modelBuilder.ApplyDietologistPersistenceModel();
        modelBuilder.ApplyCyclesPersistenceModel();
        modelBuilder.ApplyOpenFoodFactsPersistenceModel();
        modelBuilder.ApplyMarketingPersistenceModel();
    }
}
