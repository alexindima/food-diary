using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Events;

namespace FoodDiary.Modules.Users.Domain.Tests;

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
