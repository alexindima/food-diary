using FoodDiary.Results;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Domain.Entities;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public class CurrentUserAccessPolicyTests {
    [Fact]
    public void EnsureCanAccess_WithMissingUser_ReturnsInvalidToken() {
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user: null);

        Assert.NotNull(error);
        Assert.Equal("Authentication.InvalidToken", error!.Code);
    }

    [Fact]
    public void EnsureCanAccess_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted@example.com", "hash");
        user.DeleteAccount(DateTime.UtcNow);

        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);

        Assert.NotNull(error);
        Assert.Equal("Authentication.AccountDeleted", error!.Code);
    }

    [Fact]
    public void EnsureCanAccess_WithInactiveUser_ReturnsInvalidToken() {
        var user = User.Create("inactive@example.com", "hash");
        user.Deactivate();

        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(user);

        Assert.NotNull(error);
        Assert.Equal("Authentication.InvalidToken", error!.Code);
    }
}
