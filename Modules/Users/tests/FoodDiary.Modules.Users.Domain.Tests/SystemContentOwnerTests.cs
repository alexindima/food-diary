using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Users.Domain.Tests;

[ExcludeFromCodeCoverage]
public sealed class SystemContentOwnerTests {
    [Fact]
    public void CreateSystemContentOwner_ProducesStableNonAuthenticatableOwnerWithItsOwnProfileRecords() {
        var owner = User.CreateSystemContentOwner();

        Assert.Multiple(
            () => Assert.Equal(new Guid("00000000-0000-4000-8000-000000000001"), owner.Id.Value),
            () => Assert.Equal(owner.Id, User.CreateSystemContentOwner().Id),
            () => Assert.Equal(owner.Id, owner.Preferences.Id),
            () => Assert.Equal(owner.Id, owner.NutritionProfile.Id),
            () => Assert.False(owner.IsActive),
            () => Assert.Null(owner.DeletedAt),
            () => Assert.Null(owner.Email),
            () => Assert.False(owner.HasPassword),
            () => Assert.Empty(owner.Password),
            () => Assert.False(owner.MustChangePassword),
            () => Assert.False(owner.IsEmailConfirmed),
            () => Assert.Null(owner.GoogleIssuer),
            () => Assert.Null(owner.GoogleSubject),
            () => Assert.Null(owner.TelegramUserId),
            () => Assert.Null(owner.TelegramOidcIssuer),
            () => Assert.Null(owner.TelegramOidcSubject),
            () => Assert.Null(owner.PasswordResetTokenHash),
            () => Assert.Null(owner.EmailConfirmationTokenHash),
            () => Assert.Null(owner.Username),
            () => Assert.Null(owner.FirstName),
            () => Assert.Null(owner.LastName),
            () => Assert.Empty(owner.UserRoles),
            () => Assert.Empty(owner.GetRoleNames()));
    }

    [Theory]
    [InlineData("activate")]
    [InlineData("deactivate")]
    [InlineData("restore")]
    [InlineData("delete")]
    [InlineData("password")]
    [InlineData("password-reset")]
    [InlineData("email")]
    [InlineData("email-token")]
    [InlineData("reset-token")]
    [InlineData("google")]
    [InlineData("telegram")]
    [InlineData("profile")]
    [InlineData("language")]
    [InlineData("authentication")]
    [InlineData("roles")]
    [InlineData("external-roles")]
    public void SystemContentOwner_RejectsAccountAndCredentialMutations(string operation) {
        var owner = User.CreateSystemContentOwner();
        DateTime now = DateTime.UtcNow;
        Action mutate = operation switch {
            "activate" => () => owner.Activate(),
            "deactivate" => () => owner.Deactivate(),
            "restore" => () => owner.Restore(),
            "delete" => () => owner.DeleteAccount(now),
            "password" => () => owner.UpdatePassword("hash"),
            "password-reset" => () => owner.CompletePasswordReset("hash"),
            "email" => () => owner.AddVerifiedEmail("system@example.com"),
            "email-token" => () => owner.SetEmailConfirmationToken("hash", now.AddHours(1), now),
            "reset-token" => () => owner.SetPasswordResetToken("hash", now.AddHours(1), now),
            "google" => () => owner.LinkGoogleIdentity("issuer", "subject"),
            "telegram" => () => owner.LinkTelegram(123),
            "profile" => () => owner.UpdatePersonalInfo(firstName: "Changed"),
            "language" => () => owner.SetLanguage("ru"),
            "authentication" => () => owner.RecordAuthenticationActivity(now),
            "roles" => () => owner.ReplaceRoles([Role.Create("Admin")]),
            "external-roles" => () => owner.RecordExternalRoleMembershipChange(),
            _ => throw new ArgumentOutOfRangeException(nameof(operation)),
        };

        Assert.Throws<InvalidOperationException>(mutate);

        Assert.Multiple(
            () => Assert.False(owner.IsActive),
            () => Assert.Null(owner.DeletedAt),
            () => Assert.False(owner.HasPassword),
            () => Assert.Null(owner.Email),
            () => Assert.Null(owner.FirstName),
            () => Assert.Empty(owner.UserRoles),
            () => Assert.Equal(0, owner.SecurityVersion));
    }

    [Fact]
    public void UserRole_RejectsReservedContentOwnerEvenWithoutTheUserAggregate() {
        Assert.Throws<InvalidOperationException>(() => new UserRole(SystemContentOwner.Id, RoleId.New()));
    }
}
