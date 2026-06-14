using FoodDiary.Domain.Events;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Events;
using FoodDiary.Mediator;

namespace FoodDiary.Infrastructure.Tests.Events;

[ExcludeFromCodeCoverage]
public sealed class DomainEventNotificationTests {
    [Fact]
    public void Constructor_StoresDomainEventAndImplementsNotification() {
        var domainEvent = new UserDeletedDomainEvent(
            UserId.New(),
            new DateTime(2026, 6, 14, 8, 0, 0, DateTimeKind.Utc),
            new DateTime(2026, 6, 14, 8, 1, 0, DateTimeKind.Utc));

        var notification = new DomainEventNotification(domainEvent);

        Assert.Same(domainEvent, notification.DomainEvent);
        Assert.IsAssignableFrom<INotification>(notification);
    }
}
