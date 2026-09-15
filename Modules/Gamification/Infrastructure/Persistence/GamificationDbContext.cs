using FoodDiary.Modules.Gamification.PersistenceModel;
using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Gamification.Infrastructure.Persistence;

public sealed class GamificationDbContext(DbContextOptions<GamificationDbContext> options) : DbContext(options) {
    public DbSet<AchievementDefinition> AchievementDefinitions => Set<AchievementDefinition>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<AchievementEvaluationOutboxMessage> AchievementEvaluationOutbox => Set<AchievementEvaluationOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) {
        modelBuilder.ApplyGamificationPersistenceModel();
    }
}
