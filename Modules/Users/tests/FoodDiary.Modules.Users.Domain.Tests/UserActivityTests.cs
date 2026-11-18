using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserActivityTests {
    [Theory]
    [InlineData(true, DateTimeKind.Utc, "userId")]
    [InlineData(false, DateTimeKind.Local, "occurredAtUtc")]
    [InlineData(false, DateTimeKind.Unspecified, "occurredAtUtc")]
    public void Create_RequiresAUserAndUtcTime(bool emptyId, DateTimeKind kind, string parameter) {
        UserId user = emptyId ? new UserId(Guid.Empty) : UserId.New();
        var instant = new DateTime(2030, 1, 2, 0, 0, 0, kind);

        Assert.Throws<ArgumentException>(parameter, () => UserActivity.Create(user, instant));
    }
}
