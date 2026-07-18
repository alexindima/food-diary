using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Commands.GoogleLogin;
using FoodDiary.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Authentication.Models;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    private const string GoogleIssuer = "https://accounts.google.com";
    private const string GoogleSubject = "google-subject";

    [Fact]
    public async Task GoogleLoginHandler_WhenCredentialValidationFails_ReturnsFailure() {
        var tokenService = new StubAuthenticationTokenService();
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(
                new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, "google@example.com", "Alex", "User", "en"),
                validateFailure: true),
            new StubPasswordHasher(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("bad-credential"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task GoogleLoginHandler_WithDeletedExistingUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-google@example.com", "secret", hasPassword: false);
        user.LinkGoogleIdentity(GoogleIssuer, GoogleSubject);
        user.DeleteAccount(DateTime.UtcNow);
        var tokenService = new StubAuthenticationTokenService();
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(user),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")),
            new StubPasswordHasher(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(tokenService.LastUser);
        Assert.Empty(notificationRepository.Notifications);
    }

    [Fact]
    public async Task GoogleLoginHandler_WithUnlinkedPasswordAccount_RequiresExplicitLink() {
        var user = User.Create("google-password@example.com", "secret", hasPassword: true);
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(user),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.GoogleAccountLinkRequired", result.Error.Code);
        Assert.Empty(notificationRepository.Notifications);
    }

    [Fact]
    public async Task GoogleLoginHandler_ForGoogleOnlyAccount_CreatesPasswordSetupNotification() {
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, "google@example.com", "Alex", "User", "en")),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        ResultAssert.Success(result);
        Notification notification = Assert.Single(notificationRepository.Notifications);
        Assert.Equal(NotificationTypes.PasswordSetupSuggested, notification.Type);
        Assert.StartsWith("password-setup:", notification.ReferenceId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GoogleLoginHandler_DoesNotDuplicatePasswordSetupNotification() {
        var user = User.Create("google@example.com", "secret", hasPassword: false);
        user.LinkGoogleIdentity(GoogleIssuer, GoogleSubject);
        Notification existingNotification = NotificationFactory.CreatePasswordSetupSuggested(user.Id, $"password-setup:{user.Id.Value}");
        var notificationRepository = new StubNotificationRepository(existingNotification);
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(user),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, "google@example.com", "Alex", "User", "en")),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(notificationRepository.Notifications);
    }
}
