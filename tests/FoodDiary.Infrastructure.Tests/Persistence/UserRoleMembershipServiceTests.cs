using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Users;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class UserRoleMembershipServiceTests {
    [Fact]
    public async Task EnsureRoleAsync_WithEmptyUserId_ThrowsArgumentException() {
        var service = new UserRoleMembershipService(context: null!);

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.EnsureRoleAsync(UserId.Empty, RoleNames.Premium, CancellationToken.None));

        Assert.Equal("userId", ex.ParamName);
    }

    [Fact]
    public async Task EnsureRoleAsync_WithBlankRoleName_ThrowsArgumentException() {
        var service = new UserRoleMembershipService(context: null!);

        ArgumentException ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.EnsureRoleAsync(UserId.New(), " ", CancellationToken.None));

        Assert.Equal("roleName", ex.ParamName);
    }
}
