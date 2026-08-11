using FoodDiary.Domain.Entities.Achievements;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Domain.Tests.Achievements;

[ExcludeFromCodeCoverage]
public sealed class UserAchievementTests {
    private static readonly DateTime EarnedAtUtc = new(2026, 8, 9, 18, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_WithValidValues_NormalizesAndStoresAward() {
        var userId = UserId.New();

        var achievement = UserAchievement.Create(
            userId,
            " meals_10 ",
            EarnedAtUtc,
            earnedValue: 12,
            definitionVersion: 1);

        Assert.Multiple(
            () => Assert.NotEqual(UserAchievementId.Empty, achievement.Id),
            () => Assert.Equal(userId, achievement.UserId),
            () => Assert.Equal("meals_10", achievement.AchievementKey),
            () => Assert.Equal(EarnedAtUtc, achievement.EarnedAtUtc),
            () => Assert.Equal(12, achievement.EarnedValue),
            () => Assert.Equal(1, achievement.DefinitionVersion));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithInvalidKey_Throws(string achievementKey) {
        Assert.Throws<ArgumentOutOfRangeException>(() => UserAchievement.Create(
            UserId.New(),
            achievementKey,
            EarnedAtUtc,
            earnedValue: 1,
            definitionVersion: 1));
    }

    [Fact]
    public void Create_WithUnspecifiedTimestamp_NormalizesToUtc() {
        var unspecified = DateTime.SpecifyKind(EarnedAtUtc, DateTimeKind.Unspecified);

        var achievement = UserAchievement.Create(
            UserId.New(),
            "streak_3",
            unspecified,
            earnedValue: 3,
            definitionVersion: 1);

        Assert.Equal(DateTimeKind.Utc, achievement.EarnedAtUtc.Kind);
    }

    [Fact]
    public void Create_WithEmptyUserId_Throws() => Assert.Throws<ArgumentException>(() =>
        UserAchievement.Create(UserId.Empty, "streak_3", EarnedAtUtc, 3, 1));

    [Fact]
    public void Create_WithNegativeEarnedValue_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() =>
        UserAchievement.Create(UserId.New(), "streak_3", EarnedAtUtc, -1, 1));

    [Fact]
    public void Create_WithNonPositiveDefinitionVersion_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() =>
        UserAchievement.Create(UserId.New(), "streak_3", EarnedAtUtc, 3, 0));
}
