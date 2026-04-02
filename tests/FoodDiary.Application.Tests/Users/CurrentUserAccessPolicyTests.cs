using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Tests.Users;

public class CurrentUserAccessPolicyTests {
    [Fact]
    public void EnsureCanAccess_WithMissingUser_ReturnsInvalidToken() {
        var error = CurrentUserAccessPolicy.EnsureCanAccess(null);

        Assert.NotNull(error);
        Assert.Equal("Authentication.InvalidToken", error!.Code);
    }

    [Fact]
    public void EnsureCanAccess_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);

        var error = CurrentUserAccessPolicy.EnsureCanAccess(user);

        Assert.NotNull(error);
        Assert.Equal("Authentication.AccountDeleted", error!.Code);
    }

    [Fact]
    public void EnsureCanAccess_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive@example.com", "hash");
        user.Deactivate();

        var error = CurrentUserAccessPolicy.EnsureCanAccess(user);

        Assert.NotNull(error);
        Assert.Equal("Authentication.InvalidToken", error!.Code);
    }
}
