using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Achievements.Models;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Infrastructure.Persistence.Achievements;

public sealed class UserAchievementStore(FoodDiaryDbContext context) : IUserAchievementStore {
    public async Task<IReadOnlyList<UserAchievement>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        return await context.UserAchievements
            .AsNoTracking()
            .Where(achievement => achievement.UserId == userId)
            .OrderBy(achievement => achievement.EarnedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<IReadOnlyList<UserAchievement>> GrantMissingAsync(
        UserId userId,
        IReadOnlyCollection<AchievementGrantModel> grants,
        CancellationToken cancellationToken = default) {
        foreach (AchievementGrantModel grant in grants) {
            var achievement = UserAchievement.Create(
                userId,
                grant.AchievementKey,
                grant.EarnedAtUtc,
                grant.EarnedValue,
                grant.DefinitionVersion);

            await context.Database.ExecuteSqlInterpolatedAsync($$"""
                INSERT INTO "UserAchievements"
                    ("Id", "UserId", "AchievementKey", "EarnedAtUtc", "EarnedValue", "DefinitionVersion", "CreatedOnUtc")
                VALUES
                    ({{achievement.Id.Value}}, {{achievement.UserId.Value}}, {{achievement.AchievementKey}}, {{achievement.EarnedAtUtc}}, {{achievement.EarnedValue}}, {{achievement.DefinitionVersion}}, {{achievement.CreatedOnUtc}})
                ON CONFLICT ("UserId", "AchievementKey") DO NOTHING
                """, cancellationToken).ConfigureAwait(false);
        }

        return await GetByUserIdAsync(userId, cancellationToken).ConfigureAwait(false);
    }
}
