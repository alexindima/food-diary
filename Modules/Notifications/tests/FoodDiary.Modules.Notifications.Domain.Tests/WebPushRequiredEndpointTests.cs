using FoodDiary.Modules.Notifications.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Notifications.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class WebPushRequiredEndpointTests {
    [Fact]
    public void RequiredTextInputs_RejectNullWithArgumentExceptions() {
        Assert.Throws<ArgumentException>(() => WebPushSubscription.Create(
            UserId.New(),
            endpoint: null!,
            p256Dh: "p256",
            auth: "auth"));
    }
}
