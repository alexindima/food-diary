using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class PremiumTrialOverflowTests {
    [Fact]
    public void BoundaryTransitions_HandleOverflowWithoutPartialMutation() {
        var user = User.Create("trial-overflow@example.com", "hash");
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            user.StartPremiumTrial(DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc), TimeSpan.FromTicks(1)));
        Assert.Multiple(
            () => Assert.Null(user.PremiumTrialStartedAtUtc),
            () => Assert.Null(user.PremiumTrialEndsAtUtc));
    }
}
