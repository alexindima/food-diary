using System.Data.Common;
using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Achievements.Models;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.EntityFrameworkCore;

namespace FoodDiary.Modules.Gamification.Infrastructure.Persistence;

public sealed class UserAchievementStore(DbContext context, DbSet<UserAchievement> achievements, Func<DbTransaction?>? currentTransaction = null) : IUserAchievementStore {
    public async Task<IReadOnlyList<UserAchievement>> GetByUserIdAsync(
        UserId userId,
        CancellationToken cancellationToken = default) {
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
        return await achievements
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
        await SynchronizeTransactionAsync(cancellationToken).ConfigureAwait(false);
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
    private async Task SynchronizeTransactionAsync(CancellationToken cancellationToken) {
        if (currentTransaction is not null && context.Database.IsRelational()) {
            await context.Database.UseTransactionAsync(currentTransaction(), cancellationToken).ConfigureAwait(false);
        }
    }
}
