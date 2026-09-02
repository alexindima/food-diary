using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Events;

namespace FoodDiary.Domain.Tests.Domain;

[ExcludeFromCodeCoverage]
public sealed class UserLifecycleEventTests {
    [Fact]
    public void User_MarkDeleted_AndRestore_RaisesEvents() {
        var user = User.Create("events@example.com", "hash");

        user.MarkDeleted(DateTime.UtcNow);
        user.Restore();

        Assert.Contains(user.DomainEvents, e => e is UserDeletedDomainEvent);
        Assert.Contains(user.DomainEvents, e => e is UserRestoredDomainEvent);
    }
}
