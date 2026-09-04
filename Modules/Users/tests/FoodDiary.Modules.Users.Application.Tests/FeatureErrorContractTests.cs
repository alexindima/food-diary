using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class FeatureErrorContractTests {
    [Fact]
    public void UserErrors_HasOwnerAssemblyAndNamespace() {
        Assert.Multiple(
            () => Assert.Equal("FoodDiary.Modules.Users.Contracts", typeof(UserErrors).Assembly.GetName().Name),
            () => Assert.Equal("FoodDiary.Application.Abstractions.Users.Common", typeof(UserErrors).Namespace));
    }

    [Fact]
    public void UserErrors_PreservesEveryPublicErrorContract() {
        AssertError(UserErrors.NotFound(Guid.Parse("12345678-1234-1234-1234-123456789abc")), "User.NotFound", "User with ID 12345678-1234-1234-1234-123456789abc was not found.", ErrorKind.NotFound);
        AssertError(UserErrors.InvalidPassword, "User.InvalidPassword", "The current password is incorrect.", ErrorKind.Unauthorized);
        AssertError(UserErrors.PasswordNotSet, "User.PasswordNotSet", "Password is not configured for this account.", ErrorKind.Conflict);
        AssertError(UserErrors.PasswordAlreadySet, "User.PasswordAlreadySet", "Password is already configured for this account.", ErrorKind.Conflict);
        AssertError(UserErrors.AdminPasswordResetForbidden, "User.AdminPasswordResetForbidden", "Administrators cannot reset passwords for privileged accounts or their own account.", ErrorKind.Forbidden);
        AssertError(UserErrors.NotFound(), "User.NotFound", "User was not found.", ErrorKind.NotFound);
        AssertError(UserErrors.InvalidCredentials, "User.InvalidCredentials", "Invalid email or password.", ErrorKind.Unauthorized);
        AssertError(UserErrors.EmailAlreadyExists, "User.EmailAlreadyExists", "A user with this email already exists.", ErrorKind.Conflict);
    }

    private static void AssertError(Error error, string code, string message, ErrorKind kind) {
        Assert.Multiple(
            () => Assert.Equal(code, error.Code),
            () => Assert.Equal(message, error.Message),
            () => Assert.Equal(kind, error.Kind),
            () => Assert.Null(error.Details));
    }
}
