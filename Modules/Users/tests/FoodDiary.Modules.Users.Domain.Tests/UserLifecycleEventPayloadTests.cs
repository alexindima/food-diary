using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Events;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserLifecycleEventPayloadTests {
    [Fact]
    public void EventProperties_ExposeConstructorValues() {
        var userId = UserId.New();
        var occurredOnUtc = new DateTime(2026, 7, 8, 10, 0, 0, DateTimeKind.Utc);
        DateTime deletedAtUtc = occurredOnUtc.AddMinutes(-1);
        var userDeleted = new UserDeletedDomainEvent(userId, deletedAtUtc, occurredOnUtc);
        var userRestored = new UserRestoredDomainEvent(userId, occurredOnUtc);
        Assert.Multiple(
            () => Assert.Equal(userId, userDeleted.UserId),
            () => Assert.Equal(deletedAtUtc, userDeleted.DeletedAtUtc),
            () => Assert.Equal(occurredOnUtc, userDeleted.OccurredOnUtc),
            () => Assert.Equal(userId, userRestored.UserId),
            () => Assert.Equal(occurredOnUtc, userRestored.OccurredOnUtc));
    }
}
