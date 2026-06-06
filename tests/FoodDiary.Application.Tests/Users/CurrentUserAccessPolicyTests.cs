using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.Users;

namespace FoodDiary.Application.Tests.Users;

[ExcludeFromCodeCoverage]
public class CurrentUserAccessPolicyTests {
    [Fact]
    public void EnsureCanAccess_WithMissingUser_ReturnsInvalidToken() {
        Error? error = CurrentUserAccessPolicy.EnsureCanAccess(null);

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
