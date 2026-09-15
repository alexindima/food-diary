using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Modules.Gamification.PersistenceModel.Achievements;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence;

public sealed partial class FoodDiaryDbContext {
    public DbSet<AchievementDefinition> AchievementDefinitions => Set<AchievementDefinition>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<AchievementEvaluationOutboxMessage> AchievementEvaluationOutbox => Set<AchievementEvaluationOutboxMessage>();
}
