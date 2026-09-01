using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Identity.Authentication.Commands.GoogleLogin;
using FoodDiary.Application.Identity.Authentication.Commands.LinkGoogle;
using FoodDiary.Results;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Abstractions.Users.Models;
using FluentValidation.TestHelper;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    private const string GoogleIssuer = "https://accounts.google.com";
    private const string GoogleSubject = "google-subject";

    [Fact]
    public async Task GoogleLoginHandler_WhenCredentialValidationFails_ReturnsFailure() {
        var tokenService = new StubAuthenticationTokenService();
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(
                new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, "google@example.com", "Alex", "User", "en"),
                validateFailure: true),
            new StubDateTimeProvider(),
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
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")),
            new StubDateTimeProvider(),
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
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")),
            new StubDateTimeProvider(),
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
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, "google@example.com", "Alex", "User", "en")),
            new StubDateTimeProvider(),
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
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, "google@example.com", "Alex", "User", "en")),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Single(notificationRepository.Notifications);
    }

    [Fact]
    public async Task GoogleLoginHandler_ForLinkedPasswordAccount_DoesNotCreatePasswordSetupNotification() {
        var user = User.Create("linked-google@example.com", "secret", hasPassword: true);
        user.LinkGoogleIdentity(GoogleIssuer, GoogleSubject);
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            notificationRepository,
            new StubNotificationWriter(notificationRepository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Empty(notificationRepository.Notifications);
    }

    [Fact]
    public async Task LinkGoogleHandler_WithMatchingAuthenticatedAccount_LinksIdentity() {
        var user = User.Create("link-google@example.com", "secret");
        var repository = new StubUserRepository(user);
        var handler = new LinkGoogleCommandHandler(
            CreateUserAuthenticationIdentityService(repository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")));

        Result<UserModel> result =
            await handler.Handle(new LinkGoogleCommand(user.Id.Value, "credential"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(GoogleIssuer, user.GoogleIssuer);
        Assert.Equal(GoogleSubject, user.GoogleSubject);
    }

    [Fact]
    public async Task LinkGoogleHandler_WithDifferentEmail_DoesNotLinkIdentity() {
        var user = User.Create("link-google@example.com", "secret");
        var repository = new StubUserRepository(user);
        var handler = new LinkGoogleCommandHandler(
            CreateUserAuthenticationIdentityService(repository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, "other@example.com", "Alex", "User", "en")));

        Result<UserModel> result =
            await handler.Handle(new LinkGoogleCommand(user.Id.Value, "credential"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.GoogleAccountEmailMismatch", result.Error.Code);
        Assert.Null(user.GoogleSubject);
    }

    [Fact]
    public async Task LinkGoogleHandler_WhenIdentityBelongsToAnotherUser_ReturnsConflict() {
        var user = User.Create("link-google@example.com", "secret");
        var identityOwner = User.Create("owner@example.com", "secret");
        identityOwner.LinkGoogleIdentity(GoogleIssuer, GoogleSubject);
        var repository = new StubUserRepository(user, identityOwner);
        var handler = new LinkGoogleCommandHandler(
            CreateUserAuthenticationIdentityService(repository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")));

        Result<UserModel> result =
            await handler.Handle(new LinkGoogleCommand(user.Id.Value, "credential"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.GoogleIdentityAlreadyLinked", result.Error.Code);
        Assert.Null(user.GoogleSubject);
    }

    [Fact]
    public async Task LinkGoogleHandler_WhenSameIdentityIsAlreadyLinked_IsIdempotent() {
        var user = User.Create("link-google@example.com", "secret");
        user.LinkGoogleIdentity(GoogleIssuer, GoogleSubject);
        var repository = new StubUserRepository(user);
        var handler = new LinkGoogleCommandHandler(
            CreateUserAuthenticationIdentityService(repository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")));

        Result<UserModel> result =
            await handler.Handle(new LinkGoogleCommand(user.Id.Value, "credential"), CancellationToken.None);

        ResultAssert.Success(result);
    }

    [Fact]
    public async Task LinkGoogleHandler_WhenDifferentIdentityIsAlreadyLinked_DoesNotReplaceIt() {
        var user = User.Create("link-google@example.com", "secret");
        user.LinkGoogleIdentity(GoogleIssuer, "existing-subject");
        var repository = new StubUserRepository(user);
        var handler = new LinkGoogleCommandHandler(
            CreateUserAuthenticationIdentityService(repository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(GoogleIssuer, GoogleSubject, user.Email, "Alex", "User", "en")));

        Result<UserModel> result =
            await handler.Handle(new LinkGoogleCommand(user.Id.Value, "credential"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.GoogleIdentityDifferent", result.Error.Code);
        Assert.Equal("existing-subject", user.GoogleSubject);
    }

    [Fact]
    public async Task LinkGoogleHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var repository = new StubUserRepository();
        var handler = new LinkGoogleCommandHandler(
            CreateUserAuthenticationIdentityService(repository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(
                GoogleIssuer,
                GoogleSubject,
                "user@example.com",
                FirstName: null,
                LastName: null,
                Locale: null)));

        Result<UserModel> result = await handler.Handle(new LinkGoogleCommand(Guid.Empty, "credential"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task LinkGoogleHandler_WhenCredentialValidationFails_ReturnsFailure() {
        var repository = new StubUserRepository();
        var handler = new LinkGoogleCommandHandler(
            CreateUserAuthenticationIdentityService(repository),
            new StubGoogleTokenValidator(
                new GoogleIdentityPayload(
                    GoogleIssuer,
                    GoogleSubject,
                    "user@example.com",
                    FirstName: null,
                    LastName: null,
                    Locale: null),
                validateFailure: true));

        Result<UserModel> result = await handler.Handle(new LinkGoogleCommand(Guid.NewGuid(), "bad"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task LinkGoogleHandler_WhenUserAccessFails_ReturnsFailure() {
        var repository = new StubUserRepository();
        var handler = new LinkGoogleCommandHandler(
            CreateUserAuthenticationIdentityService(repository),
            new StubGoogleTokenValidator(new GoogleIdentityPayload(
                GoogleIssuer,
                GoogleSubject,
                "user@example.com",
                FirstName: null,
                LastName: null,
                Locale: null)));

        Result<UserModel> result = await handler.Handle(new LinkGoogleCommand(Guid.NewGuid(), "credential"), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public void LinkGoogleCommandValidator_ValidatesRequiredFields() {
        var validator = new LinkGoogleCommandValidator();

        TestValidationResult<LinkGoogleCommand> invalid = validator.TestValidate(new LinkGoogleCommand(Guid.Empty, ""));
        TestValidationResult<LinkGoogleCommand> valid = validator.TestValidate(new LinkGoogleCommand(Guid.NewGuid(), "credential"));

        invalid.ShouldHaveValidationErrorFor(command => command.UserId).WithErrorCode("Validation.Required");
        invalid.ShouldHaveValidationErrorFor(command => command.Credential).WithErrorCode("Validation.Required");
        valid.ShouldNotHaveAnyValidationErrors();
    }
}
