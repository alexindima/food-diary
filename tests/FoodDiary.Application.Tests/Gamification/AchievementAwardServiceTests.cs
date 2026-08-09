using FoodDiary.Application.Abstractions.Achievements.Common;
using FoodDiary.Application.Abstractions.Achievements.Models;
using FoodDiary.Application.Gamification.Models;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Gamification;

[ExcludeFromCodeCoverage]
public sealed class AchievementAwardServiceTests {
    private static readonly DateTime NowUtc = new(2026, 8, 9, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task EvaluateAndGrantAsync_GrantsEligibleAwardsWithEarnedMetadata() {
        var store = new InMemoryUserAchievementStore();
        AchievementAwardService service = CreateService(store);

        IReadOnlyList<BadgeModel> badges = await service.EvaluateAndGrantAsync(
            UserId.New(),
            longestStreak: 3,
            totalMeals: 10,
            cancellationToken: CancellationToken.None);

        BadgeModel streak = badges.Single(badge => string.Equals(badge.Key, "streak_3", StringComparison.Ordinal));
        BadgeModel meals = badges.Single(badge => string.Equals(badge.Key, "meals_10", StringComparison.Ordinal));
        Assert.Multiple(
            () => Assert.True(streak.IsEarned),
            () => Assert.Equal(NowUtc, streak.EarnedAtUtc),
            () => Assert.True(meals.IsEarned),
            () => Assert.Equal(NowUtc, meals.EarnedAtUtc),
            () => Assert.Equal(2, store.Achievements.Count));
    }

    [Fact]
    public async Task EvaluateAndGrantAsync_WhenRepeated_IsIdempotent() {
        var store = new InMemoryUserAchievementStore();
        AchievementAwardService service = CreateService(store);
        var userId = UserId.New();

        await service.EvaluateAndGrantAsync(userId, longestStreak: 3, totalMeals: 10, cancellationToken: CancellationToken.None);
        await service.EvaluateAndGrantAsync(userId, longestStreak: 3, totalMeals: 10, cancellationToken: CancellationToken.None);

        Assert.Equal(2, store.Achievements.Count);
    }

    [Fact]
    public async Task EvaluateAndGrantAsync_WhenMetricDrops_DoesNotRevokeAward() {
        var store = new InMemoryUserAchievementStore();
        AchievementAwardService service = CreateService(store);
        var userId = UserId.New();

        await service.EvaluateAndGrantAsync(userId, longestStreak: 3, totalMeals: 0, cancellationToken: CancellationToken.None);
        IReadOnlyList<BadgeModel> afterDrop = await service.EvaluateAndGrantAsync(
            userId,
            longestStreak: 0,
            totalMeals: 0,
            cancellationToken: CancellationToken.None);

        Assert.True(afterDrop.Single(badge => string.Equals(badge.Key, "streak_3", StringComparison.Ordinal)).IsEarned);
    }

    [Fact]
    public async Task EvaluateAndGrantAsync_WhenEarnedDefinitionIsDisabled_KeepsRetiredAwardVisible() {
        var store = new InMemoryUserAchievementStore();
        var definitionStore = new InMemoryAchievementDefinitionStore();
        var service = new AchievementAwardService(store, definitionStore, new StubTimeProvider());
        var userId = UserId.New();
        await service.EvaluateAndGrantAsync(userId, longestStreak: 3, totalMeals: 0, cancellationToken: CancellationToken.None);
        AchievementDefinition definition = definitionStore.Definitions.Single(
            item => string.Equals(item.Key, "streak_3", StringComparison.Ordinal));
        definition.Update(definition.Category, definition.Metric, definition.Threshold, definition.TitleRu, definition.TitleEn,
            definition.DescriptionRu, definition.DescriptionEn, definition.Icon, definition.SortOrder, isActive: false);

        IReadOnlyList<BadgeModel> badges = await service.EvaluateAndGrantAsync(
            userId, longestStreak: 0, totalMeals: 0, cancellationToken: CancellationToken.None);

        Assert.True(badges.Single(item => string.Equals(item.Key, "streak_3", StringComparison.Ordinal)).IsEarned);
    }

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryUserAchievementStore : IUserAchievementStore {
        public List<UserAchievement> Achievements { get; } = [];

        public Task<IReadOnlyList<UserAchievement>> GetByUserIdAsync(
            UserId userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<UserAchievement>>(Achievements.Where(item => item.UserId == userId).ToList());

        public Task<IReadOnlyList<UserAchievement>> GrantMissingAsync(
            UserId userId,
            IReadOnlyCollection<AchievementGrantModel> grants,
            CancellationToken cancellationToken = default) {
            foreach (AchievementGrantModel grant in grants) {
                if (Achievements.Any(item => item.UserId == userId && string.Equals(item.AchievementKey, grant.AchievementKey, StringComparison.Ordinal))) {
                    continue;
                }

                Achievements.Add(UserAchievement.Create(
                    userId,
                    grant.AchievementKey,
                    grant.EarnedAtUtc,
                    grant.EarnedValue,
                    grant.DefinitionVersion));
            }

            return GetByUserIdAsync(userId, cancellationToken);
        }
    }

    private static AchievementAwardService CreateService(IUserAchievementStore store) =>
        new(store, new InMemoryAchievementDefinitionStore(), new StubTimeProvider());

    [ExcludeFromCodeCoverage]
    private sealed class InMemoryAchievementDefinitionStore : IAchievementDefinitionStore {
        public IReadOnlyList<AchievementDefinition> Definitions { get; } = [
            AchievementDefinition.Create("streak_3", "streak", AchievementMetric.LongestStreak, 3, "Streak 3 RU", "Streak 3", "Description RU", "Description", "fire", 1),
            AchievementDefinition.Create("meals_10", "meals", AchievementMetric.TotalMeals, 10, "10 meals RU", "10 meals", "Description RU", "Description", "restaurant", 2),
        ];

        public Task<IReadOnlyList<AchievementDefinition>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult(Definitions);
        public Task<IReadOnlyList<AchievementDefinition>> GetActiveAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<AchievementDefinition>>(Definitions.Where(item => item.IsActive).ToList());
        public Task<AchievementDefinition?> GetByIdTrackingAsync(AchievementDefinitionId id, CancellationToken cancellationToken = default) => Task.FromResult<AchievementDefinition?>(null);
        public Task<bool> TryAddAsync(AchievementDefinition definition, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task UpdateAsync(AchievementDefinition definition, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(NowUtc);
    }
}
