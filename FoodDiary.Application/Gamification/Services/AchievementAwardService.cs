using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Achievements.Models;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Gamification.Services;

public sealed class AchievementAwardService(
    IUserAchievementStore achievementStore,
    IAchievementDefinitionStore definitionStore,
    TimeProvider timeProvider) : IAchievementAwardService {
    public async Task<IReadOnlyList<BadgeModel>> EvaluateAndGrantAsync(
        UserId userId,
        int longestStreak,
        int totalMeals,
        CancellationToken cancellationToken = default,
        DateTime? earnedAtUtc = null) {
        IReadOnlyList<UserAchievement> existing = await achievementStore
            .GetByUserIdAsync(userId, cancellationToken)
            .ConfigureAwait(false);
        var existingKeys = existing
            .Select(achievement => achievement.AchievementKey)
            .ToHashSet(StringComparer.Ordinal);
        IReadOnlyList<AchievementDefinition> definitions = await definitionStore.GetAllAsync(cancellationToken).ConfigureAwait(false);
        IReadOnlyList<AchievementDefinition> visibleDefinitions = definitions
            .Where(definition => definition.IsActive || existingKeys.Contains(definition.Key))
            .ToList();
        IReadOnlyList<BadgeModel> eligibility = GamificationCalculator.CalculateBadges(visibleDefinitions, longestStreak, totalMeals);
        DateTime normalizedEarnedAtUtc = earnedAtUtc ?? timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyDictionary<string, AchievementDefinition> definitionsByKey = visibleDefinitions
            .ToDictionary(definition => definition.Key, StringComparer.Ordinal);

        AchievementGrantModel[] missingGrants = [.. eligibility
            .Where(badge => definitionsByKey[badge.Key].IsActive && badge.IsEarned && !existingKeys.Contains(badge.Key))
            .Select(badge => {
                AchievementDefinition definition = definitionsByKey[badge.Key];
                int earnedValue = GamificationCalculator.GetMetricValue(definition.Metric, longestStreak, totalMeals);
                return new AchievementGrantModel(
                    badge.Key,
                    normalizedEarnedAtUtc,
                    earnedValue,
                    definition.Version);
            })];

        IReadOnlyList<UserAchievement> persisted = missingGrants.Length == 0
            ? existing
            : await achievementStore.GrantMissingAsync(userId, missingGrants, cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<string, UserAchievement> persistedByKey = persisted
            .ToDictionary(achievement => achievement.AchievementKey, StringComparer.Ordinal);

        return eligibility
            .Select(badge => persistedByKey.TryGetValue(badge.Key, out UserAchievement? achievement)
                ? badge with {
                    IsEarned = true,
                    EarnedAtUtc = achievement.EarnedAtUtc,
                }
                : badge with { IsEarned = false })
            .ToList();
    }
}
