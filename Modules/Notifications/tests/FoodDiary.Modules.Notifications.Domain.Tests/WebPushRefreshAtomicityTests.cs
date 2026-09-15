using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class WebPushRefreshAtomicityTests {
    [Fact]
    public void EntityUpdates_WhenLateValidationFails_AreAtomic() {
        var originalUserId = UserId.New();
        var subscription = WebPushSubscription.Create(originalUserId, "https://push.example", "p256", "auth");
        Assert.Throws<ArgumentOutOfRangeException>(() => subscription.Refresh(
            "changed",
            new string('x', 513)));
        Assert.Multiple(
            () => Assert.Equal(originalUserId, subscription.UserId),
            () => Assert.Equal("p256", subscription.P256Dh));
    }
}
