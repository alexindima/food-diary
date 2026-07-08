using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationUserAccessPolicyTests {
    [Fact]
    public void EnsureCanAuthenticate_WithNullUser_ReturnsInvalidCredentials() {
        Error? error = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user: null);

        Assert.NotNull(error);
        Assert.Equal("Authentication.InvalidCredentials", error!.Code);
    }

    [Fact]
    public void EnsureCanAuthenticate_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted@example.com", "password-hash");
        user.DeleteAccount(DateTime.UtcNow);

        Error? error = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);

        Assert.NotNull(error);
        Assert.Equal("Authentication.AccountDeleted", error!.Code);
    }

    [Fact]
    public void EnsureCanAuthenticate_WithInactiveUser_ReturnsInvalidCredentials() {
        var user = User.Create("inactive@example.com", "password-hash");
        user.Deactivate();

        Error? error = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);

        Assert.NotNull(error);
        Assert.Equal("Authentication.InvalidCredentials", error!.Code);
    }

    [Fact]
    public void EnsureCanAuthenticate_WithActiveUser_ReturnsNull() {
        var user = User.Create("active@example.com", "password-hash");

        Error? error = AuthenticationUserAccessPolicy.EnsureCanAuthenticate(user);

        Assert.Null(error);
    }

    [Fact]
    public void CanRequestPasswordReset_AllowsOnlyActiveNonDeletedUser() {
        var activeUser = User.Create("active@example.com", "password-hash");
        var inactiveUser = User.Create("inactive@example.com", "password-hash");
        inactiveUser.Deactivate();
        var deletedUser = User.Create("deleted@example.com", "password-hash");
        deletedUser.DeleteAccount(DateTime.UtcNow);

        Assert.True(AuthenticationUserAccessPolicy.CanRequestPasswordReset(activeUser));
        Assert.False(AuthenticationUserAccessPolicy.CanRequestPasswordReset(user: null));
        Assert.False(AuthenticationUserAccessPolicy.CanRequestPasswordReset(inactiveUser));
        Assert.False(AuthenticationUserAccessPolicy.CanRequestPasswordReset(deletedUser));
    }
}
