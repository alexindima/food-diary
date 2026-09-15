using FoodDiary.Modules.Gamification.Domain.Entities.Achievements;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Gamification.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class AchievementRequiredKeyTests {
    private static readonly DateTime Now = new(2026, 8, 19, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void RequiredTextInputs_RejectNullWithArgumentExceptions() {
        Assert.Throws<ArgumentNullException>(() => UserAchievement.Create(
            UserId.New(),
            achievementKey: null!,
            Now,
            earnedValue: 1,
            definitionVersion: 1));
    }
}
