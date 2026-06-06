using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Authentication.Commands.AdminSsoExchange;
using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Authentication.Commands.GoogleLogin;
using FoodDiary.Application.Authentication.Commands.LinkTelegram;
using FoodDiary.Application.Authentication.Commands.Login;
using FoodDiary.Application.Authentication.Commands.Register;
using FoodDiary.Application.Authentication.Commands.RequestPasswordReset;
using FoodDiary.Application.Authentication.Commands.RestoreAccount;
using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Authentication.Commands.TelegramBotAuth;
using FoodDiary.Application.Authentication.Commands.TelegramLoginWidget;
using FoodDiary.Application.Authentication.Commands.TelegramVerify;
using FoodDiary.Application.Authentication.Commands.VerifyEmail;
using FoodDiary.Application.Authentication.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Audit;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Users.Models;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationCommandHandlerTests {
    [Fact]
    public async Task AdminSsoStartHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new AdminSsoStartCommandHandler(new StubAdminSsoService(), new StubUserRepository());

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(Guid.Empty), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WithNonAdminUser_ReturnsForbiddenWithoutCreatingCode() {
        var user = User.Create("user@example.com", "secret");
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(adminSsoService, new StubUserRepository(user));

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AdminSsoForbidden", result.Error.Code);
        Assert.Equal(0, adminSsoService.CreateCodeCallCount);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WithMissingUser_ReturnsInvalidCredentials() {
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(adminSsoService, new StubUserRepository());

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidCredentials", result.Error.Code);
        Assert.Equal(0, adminSsoService.CreateCodeCallCount);
    }

    [Fact]
    public async Task AdminSsoStartHandler_WithAdminUser_CreatesCode() {
        var user = User.Create("admin@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        var adminSsoService = new StubAdminSsoService();
        var handler = new AdminSsoStartCommandHandler(adminSsoService, new StubUserRepository(user));

        Result<AdminSsoStartModel> result = await handler.Handle(new AdminSsoStartCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("admin-sso-code", result.Value.Code);
        Assert.Equal(1, adminSsoService.CreateCodeCallCount);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithInvalidCode_ReturnsFailure() {
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(),
            new StubUserRepository(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("bad-code"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AdminSsoInvalidCode", result.Error.Code);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithMissingUser_ReturnsNotFound() {
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(UserId.New()),
            new StubUserRepository(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-admin-sso@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        user.DeleteAccount(DateTime.UtcNow);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(user.Id),
            new DirectUserByIdRepository(user),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithNonAdminUser_ReturnsForbidden() {
        var user = User.Create("client@example.com", "secret");
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(user.Id),
            new StubUserRepository(user),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AdminSsoForbidden", result.Error.Code);
    }

    [Fact]
    public async Task AdminSsoExchangeHandler_WithAdminUser_IssuesTokens() {
        var user = User.Create("admin-exchange@example.com", "secret");
        user.ReplaceRoles([Role.Create(RoleNames.Admin)]);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new AdminSsoExchangeCommandHandler(
            new StubAdminSsoService(user.Id),
            new StubUserRepository(user),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new AdminSsoExchangeCommand("admin-sso-code"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }

    [Fact]
    public async Task RegisterHandler_CreatesUserIssuesTokensAndSendsVerificationEmail() {
        var sender = new StubEmailSender();
        var tokens = new StubAuthenticationTokenService();
        var handler = new RegisterCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            sender,
            new StubDateTimeProvider(),
            tokens,
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RegisterCommandHandler>.Instance);

        Result<AuthenticationModel> result = await handler.Handle(
            new RegisterCommand("new@example.com", "secret", "ru", "https://client.test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal("refresh", result.Value.RefreshToken);
        Assert.Equal("new@example.com", sender.LastEmailVerification?.ToEmail);
        Assert.Equal("https://client.test", sender.LastEmailVerification?.ClientOrigin);
        Assert.Equal("ru", tokens.LastUser?.Language);
        Assert.NotNull(tokens.LastUser?.EmailConfirmationTokenHash);
    }

    [Fact]
    public async Task RegisterHandler_WhenVerificationEmailFails_StillReturnsTokens() {
        var handler = new RegisterCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubEmailSender(throwOnEmailVerification: true),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RegisterCommandHandler>.Instance);

        Result<AuthenticationModel> result = await handler.Handle(
            new RegisterCommand("email-failure@example.com", "secret", Language: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
    }

    [Fact]
    public async Task LoginHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-login@example.com", "secret");
        user.DeleteAccount(DateTime.UtcNow);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new LoginCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new LoginCommand(user.Email, "secret"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task LoginHandler_WithValidCredentials_IssuesTokens() {
        var user = User.Create("login@example.com", "secret");
        var tokenService = new StubAuthenticationTokenService();
        var handler = new LoginCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new LoginCommand(user.Email, "secret"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WhenUserMissing_ReturnsSuccessWithoutSending() {
        var sender = new StubEmailSender();
        var handler = new RequestPasswordResetCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            sender,
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RequestPasswordResetCommandHandler>.Instance);

        Result result = await handler.Handle(new RequestPasswordResetCommand("missing@example.com"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(sender.LastPasswordReset);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WhenRequestIsInCooldown_ReturnsSuccessWithoutSending() {
        var user = User.Create("cooldown-reset@example.com", "secret");
        DateTime nowUtc = new StubDateTimeProvider().GetUtcNow().UtcDateTime;
        user.SetPasswordResetToken(new UserTokenIssue("old-hash", nowUtc.AddHours(1), nowUtc.AddSeconds(-30)));
        var sender = new StubEmailSender();
        var handler = new RequestPasswordResetCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            sender,
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RequestPasswordResetCommandHandler>.Instance);

        Result result = await handler.Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(sender.LastPasswordReset);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WithActiveUser_UpdatesTokenAndSendsMessage() {
        var user = User.Create("reset@example.com", "secret");
        var sender = new StubEmailSender();
        var handler = new RequestPasswordResetCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            sender,
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RequestPasswordResetCommandHandler>.Instance);

        Result result = await handler.Handle(
            new RequestPasswordResetCommand(user.Email, "https://client.test"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.PasswordResetTokenHash);
        Assert.Equal(user.Email, sender.LastPasswordReset?.ToEmail);
        Assert.Equal("https://client.test", sender.LastPasswordReset?.ClientOrigin);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WhenEmailFails_StillReturnsSuccess() {
        var user = User.Create("reset-email-fails@example.com", "secret");
        var handler = new RequestPasswordResetCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubEmailSender(throwOnPasswordReset: true),
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RequestPasswordResetCommandHandler>.Instance);

        Result result = await handler.Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.PasswordResetTokenHash);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(Guid.Empty, "token", "StrongPass123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyEmailHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(Guid.Empty, "token"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenUserMissing_ReturnsNotFound() {
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(Guid.NewGuid(), "token"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenAlreadyConfirmed_ReturnsSuccess() {
        var user = User.Create("confirmed-verify@example.com", "secret");
        user.SetEmailConfirmed(isConfirmed: true);
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenTokenMissing_ReturnsInvalidToken() {
        var user = User.Create("missing-token@example.com", "secret");
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenTokenDoesNotMatch_ReturnsInvalidToken() {
        var user = User.Create("bad-token@example.com", "secret");
        user.SetEmailConfirmationToken(new UserTokenIssue("expected", DateTime.UtcNow.AddHours(1), DateTime.UtcNow));
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "actual"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task VerifyEmailHandler_WithValidToken_CompletesVerification() {
        var user = User.Create("verify@example.com", "secret");
        var dateTimeProvider = new StubDateTimeProvider();
        user.SetEmailConfirmationToken(new UserTokenIssue("token", dateTimeProvider.GetUtcNow().UtcDateTime.AddHours(1), dateTimeProvider.GetUtcNow().UtcDateTime));
        var notifier = new StubEmailVerificationNotifier();
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            dateTimeProvider,
            notifier);

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsEmailConfirmed);
        Assert.Equal(user.Id.Value, notifier.LastUserId);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenNotifierFails_StillReturnsSuccess() {
        var user = User.Create("verify-notifier-fails@example.com", "secret");
        var dateTimeProvider = new StubDateTimeProvider();
        user.SetEmailConfirmationToken(new UserTokenIssue("token", dateTimeProvider.GetUtcNow().UtcDateTime.AddHours(1), dateTimeProvider.GetUtcNow().UtcDateTime));
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            dateTimeProvider,
            new StubEmailVerificationNotifier(throwOnNotify: true));

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsEmailConfirmed);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new ResendEmailVerificationCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubEmailSender(),
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ResendEmailVerificationCommandHandler>.Instance);

        Result result = await handler.Handle(
            new ResendEmailVerificationCommand(Guid.Empty),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WhenEmailAlreadyConfirmed_ReturnsSuccessWithoutSending() {
        var user = User.Create("confirmed@example.com", "secret");
        user.SetEmailConfirmed(isConfirmed: true);
        var sender = new StubEmailSender();
        ResendEmailVerificationCommandHandler handler = CreateResendEmailVerificationHandler(user, sender);

        Result result = await handler.Handle(new ResendEmailVerificationCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(sender.LastEmailVerification);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WhenSentRecently_ReturnsCooldownFailure() {
        var user = User.Create("cooldown@example.com", "secret");
        user.SetEmailConfirmationToken(new UserTokenIssue("old-hash", DateTime.UtcNow.AddHours(1), new StubDateTimeProvider().GetUtcNow().UtcDateTime.AddSeconds(-30)));
        ResendEmailVerificationCommandHandler handler = CreateResendEmailVerificationHandler(user, new StubEmailSender());

        Result result = await handler.Handle(new ResendEmailVerificationCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("recently", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WithMissingUser_ReturnsInvalidToken() {
        var handler = new ResendEmailVerificationCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubEmailSender(),
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ResendEmailVerificationCommandHandler>.Instance);

        Result result = await handler.Handle(new ResendEmailVerificationCommand(Guid.NewGuid()), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WhenPreviousSendIsOutsideCooldown_SendsMessage() {
        var user = User.Create("cooldown-expired@example.com", "secret");
        DateTime nowUtc = new StubDateTimeProvider().GetUtcNow().UtcDateTime;
        user.SetEmailConfirmationToken(new UserTokenIssue("old-hash", nowUtc.AddHours(1), nowUtc.AddMinutes(-5)));
        var sender = new StubEmailSender();
        ResendEmailVerificationCommandHandler handler = CreateResendEmailVerificationHandler(user, sender);

        Result result = await handler.Handle(new ResendEmailVerificationCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(sender.LastEmailVerification);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WithActiveUnconfirmedUser_UpdatesTokenAndSendsMessage() {
        var user = User.Create("resend@example.com", "secret");
        var sender = new StubEmailSender();
        ResendEmailVerificationCommandHandler handler = CreateResendEmailVerificationHandler(user, sender);

        Result result = await handler.Handle(new ResendEmailVerificationCommand(user.Id.Value, "https://client.test"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(user.EmailConfirmationTokenHash);
        Assert.NotNull(sender.LastEmailVerification);
        Assert.Equal("resend@example.com", sender.LastEmailVerification.ToEmail);
        Assert.Equal("https://client.test", sender.LastEmailVerification.ClientOrigin);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WhenSenderFails_ReturnsValidationFailure() {
        var user = User.Create("send-fails@example.com", "secret");
        ResendEmailVerificationCommandHandler handler = CreateResendEmailVerificationHandler(user, new StubEmailSender(throwOnEmailVerification: true));

        Result result = await handler.Handle(new ResendEmailVerificationCommand(user.Id.Value), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("Failed to send", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LinkTelegramHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new LinkTelegramCommandHandler(
            new StubUserRepository(),
            new StubTelegramAuthValidator());

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(Guid.Empty, "init-data"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenInitDataInvalid_ReturnsFailure() {
        var user = User.Create("link-invalid@example.com", "secret");
        var handler = new LinkTelegramCommandHandler(
            new StubUserRepository(user),
            new StubTelegramAuthValidator(validateFailure: true));

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "bad-init-data"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenUserMissing_ReturnsFailure() {
        var handler = new LinkTelegramCommandHandler(
            new StubUserRepository(),
            new StubTelegramAuthValidator());

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(Guid.NewGuid(), "valid-init-data"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenSameTelegramIdAlreadyLinked_ReturnsCurrentUser() {
        var user = User.Create("same-telegram@example.com", "secret");
        user.LinkTelegram(123456);
        var handler = new LinkTelegramCommandHandler(
            new StubUserRepository(user),
            new StubTelegramAuthValidator());

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "valid-init-data"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(123456, user.TelegramUserId);
    }

    [Fact]
    public async Task LinkTelegramHandler_WhenTelegramIdBelongsToAnotherUser_ReturnsFailure() {
        var user = User.Create("current-telegram@example.com", "secret");
        var existing = User.Create("existing-telegram@example.com", "secret");
        existing.LinkTelegram(123456);
        var handler = new LinkTelegramCommandHandler(
            new StubUserRepository(user, existing),
            new StubTelegramAuthValidator());

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "valid-init-data"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramAlreadyLinked", result.Error.Code);
    }

    [Fact]
    public async Task LinkTelegramHandler_WithAvailableTelegramId_LinksCurrentUser() {
        var user = User.Create("link@example.com", "secret");
        var handler = new LinkTelegramCommandHandler(
            new StubUserRepository(user),
            new StubTelegramAuthValidator());

        Result<UserModel> result = await handler.Handle(
            new LinkTelegramCommand(user.Id.Value, "valid-init-data"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(123456, user.TelegramUserId);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenUserMissing_ReturnsNotFound() {
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(Guid.NewGuid(), "token", "StrongPass123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenTokenMissing_ReturnsInvalidToken() {
        var user = User.Create("reset-missing-token@example.com", "secret");
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "token", "StrongPass123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenTokenDoesNotMatch_ReturnsInvalidToken() {
        var user = User.Create("reset-bad-token@example.com", "secret");
        user.SetPasswordResetToken(new UserTokenIssue("expected", DateTime.UtcNow.AddHours(1), DateTime.UtcNow));
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "actual", "StrongPass123"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WithValidToken_ChangesPasswordAndIssuesTokens() {
        var user = User.Create("reset-valid@example.com", "old-password");
        var dateTimeProvider = new StubDateTimeProvider();
        user.SetPasswordResetToken(new UserTokenIssue("token", dateTimeProvider.GetUtcNow().UtcDateTime.AddHours(1), dateTimeProvider.GetUtcNow().UtcDateTime));
        var tokenService = new StubAuthenticationTokenService();
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            dateTimeProvider,
            tokenService,
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "token", "new-password"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }

    [Fact]
    public async Task TelegramVerifyHandler_WhenInitDataInvalid_ReturnsFailure() {
        var handler = new TelegramVerifyCommandHandler(
            new StubUserRepository(),
            new StubTelegramAuthValidator(validateFailure: true),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new TelegramVerifyCommand("bad-init-data"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task TelegramVerifyHandler_WhenTelegramUserIsNotLinked_ReturnsFailure() {
        var handler = new TelegramVerifyCommandHandler(
            new StubUserRepository(),
            new StubTelegramAuthValidator(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new TelegramVerifyCommand("valid-init-data"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramNotLinked", result.Error.Code);
    }

    [Fact]
    public async Task TelegramVerifyHandler_WithLinkedUser_IssuesTokens() {
        var user = User.Create("telegram@example.com", "secret");
        user.LinkTelegram(123456);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new TelegramVerifyCommandHandler(
            new StubUserRepository(user),
            new StubTelegramAuthValidator(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new TelegramVerifyCommand("valid-init-data"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }

    [Fact]
    public async Task TelegramLoginWidgetHandler_WhenWidgetDataInvalid_ReturnsFailure() {
        var handler = new TelegramLoginWidgetCommandHandler(
            new StubUserRepository(),
            new StubTelegramLoginWidgetValidator(validateFailure: true),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(
            new TelegramLoginWidgetCommand(123456, 123, "bad-hash", Username: null, FirstName: null, LastName: null, PhotoUrl: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task TelegramLoginWidgetHandler_WhenTelegramUserIsNotLinked_ReturnsFailure() {
        var handler = new TelegramLoginWidgetCommandHandler(
            new StubUserRepository(),
            new StubTelegramLoginWidgetValidator(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(
            new TelegramLoginWidgetCommand(123456, 123, "hash", Username: null, FirstName: null, LastName: null, PhotoUrl: null),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramNotLinked", result.Error.Code);
    }

    [Fact]
    public async Task TelegramLoginWidgetHandler_WithLinkedUser_IssuesTokens() {
        var user = User.Create("telegram-widget@example.com", "secret");
        user.LinkTelegram(123456);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new TelegramLoginWidgetCommandHandler(
            new StubUserRepository(user),
            new StubTelegramLoginWidgetValidator(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new TelegramLoginWidgetCommand(123456, 123, "hash", "alex", "Alex", "User", PhotoUrl: null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }

    [Fact]
    public async Task TelegramBotAuthHandler_WhenTelegramUserIsNotLinked_ReturnsFailure() {
        var handler = new TelegramBotAuthCommandHandler(
            new StubUserRepository(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new TelegramBotAuthCommand(123456), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.TelegramNotLinked", result.Error.Code);
    }

    [Fact]
    public async Task TelegramBotAuthHandler_WithLinkedUser_IssuesTokens() {
        var user = User.Create("telegram-bot@example.com", "secret");
        user.LinkTelegram(123456);
        var tokenService = new StubAuthenticationTokenService();
        var handler = new TelegramBotAuthCommandHandler(
            new StubUserRepository(user),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new TelegramBotAuthCommand(123456), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Equal(user, tokenService.LastUser);
    }

    [Fact]
    public async Task RestoreAccountHandler_WithDeletedUser_RestoresAndIssuesTokens() {
        var user = User.Create("deleted@example.com", "secret");
        user.DeleteAccount(DateTime.UtcNow.AddDays(-2));
        var handler = new RestoreAccountCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService(),
            new StubDateTimeProvider());

        Result<AuthenticationModel> result = await handler.Handle(
            new RestoreAccountCommand(user.Email, "secret"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(user.IsActive);
        Assert.Null(user.DeletedAt);
    }

    [Fact]
    public async Task RestoreAccountHandler_WithMissingUser_ReturnsInvalidCredentials() {
        var handler = new RestoreAccountCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService(),
            new StubDateTimeProvider());

        Result<AuthenticationModel> result = await handler.Handle(
            new RestoreAccountCommand("missing@example.com", "secret"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task RestoreAccountHandler_WithActiveUser_ReturnsAccountNotDeleted() {
        var user = User.Create("active-restore@example.com", "secret");
        var handler = new RestoreAccountCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService(),
            new StubDateTimeProvider());

        Result<AuthenticationModel> result = await handler.Handle(
            new RestoreAccountCommand(user.Email, "secret"),
            CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountNotDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GoogleLoginHandler_WhenCredentialValidationFails_ReturnsFailure() {
        var tokenService = new StubAuthenticationTokenService();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(),
            new StubNotificationRepository(),
            new StubGoogleTokenValidator(
                new GoogleIdentityPayload("google@example.com", "Alex", "User", "en"),
                validateFailure: true),
            new StubPasswordHasher(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("bad-credential"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Null(tokenService.LastUser);
    }

    [Fact]
    public async Task GoogleLoginHandler_WithDeletedExistingUser_ReturnsAccountDeleted() {
        var user = User.Create("deleted-google@example.com", "secret", hasPassword: false);
        user.DeleteAccount(DateTime.UtcNow);
        var tokenService = new StubAuthenticationTokenService();
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(user),
            notificationRepository,
            new StubGoogleTokenValidator(new GoogleIdentityPayload(user.Email, "Alex", "User", "en")),
            new StubPasswordHasher(),
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
        Assert.Null(tokenService.LastUser);
        Assert.Empty(notificationRepository.Notifications);
    }

    [Fact]
    public async Task GoogleLoginHandler_WithPasswordAccount_DoesNotCreatePasswordSetupNotification() {
        var user = User.Create("google-password@example.com", "secret", hasPassword: true);
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(user),
            notificationRepository,
            new StubGoogleTokenValidator(new GoogleIdentityPayload(user.Email, "Alex", "User", "en")),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(notificationRepository.Notifications);
    }

    [Fact]
    public async Task GoogleLoginHandler_ForGoogleOnlyAccount_CreatesPasswordSetupNotification() {
        var notificationRepository = new StubNotificationRepository();
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(),
            notificationRepository,
            new StubGoogleTokenValidator(new GoogleIdentityPayload("google@example.com", "Alex", "User", "en")),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Notification notification = Assert.Single(notificationRepository.Notifications);
        Assert.Equal(NotificationTypes.PasswordSetupSuggested, notification.Type);
        Assert.StartsWith("password-setup:", notification.ReferenceId, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GoogleLoginHandler_DoesNotDuplicatePasswordSetupNotification() {
        var user = User.Create("google@example.com", "secret", hasPassword: false);
        Notification existingNotification = NotificationFactory.CreatePasswordSetupSuggested(user.Id, $"password-setup:{user.Id.Value}");
        var notificationRepository = new StubNotificationRepository(existingNotification);
        var handler = new GoogleLoginCommandHandler(
            new StubUserRepository(user),
            notificationRepository,
            new StubGoogleTokenValidator(new GoogleIdentityPayload("google@example.com", "Alex", "User", "en")),
            new StubPasswordHasher(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(new GoogleLoginCommand("credential"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(notificationRepository.Notifications);
    }

    [ExcludeFromCodeCoverage]
    private sealed class NullAuditLogger : IAuditLogger {
        public void Log(string action, UserId actorId, string? targetType = null, string? targetId = null, string? details = null) { }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubAdminSsoService(UserId? exchangeUserId = null) : IAdminSsoService {
        public int CreateCodeCallCount { get; private set; }

        public Task<AdminSsoCode> CreateCodeAsync(UserId userId, CancellationToken cancellationToken = default) {
            CreateCodeCallCount++;
            return Task.FromResult(new AdminSsoCode("admin-sso-code", DateTime.UtcNow.AddMinutes(5)));
        }

        public Task<UserId?> ExchangeCodeAsync(string code, CancellationToken cancellationToken = default) =>
            Task.FromResult(string.Equals(code, "admin-sso-code", StringComparison.Ordinal) ? exchangeUserId : null);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubUserRepository(User? user = null, params User[] otherUsers) : IUserRepository {
        private readonly List<User> _users = user is null ? [.. otherUsers] : [user, .. otherUsers];

        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(_users.FirstOrDefault(candidate => string.Equals(candidate.Email, email, StringComparison.OrdinalIgnoreCase)));
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(_users.FirstOrDefault(candidate => candidate is { IsActive: true, DeletedAt: null } && candidate.Id == id));
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(_users.FirstOrDefault(candidate =>
                candidate is { IsActive: true, DeletedAt: null, TelegramUserId: not null } &&
                candidate.TelegramUserId == telegramUserId));
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) =>
            Task.FromResult<User?>(_users.FirstOrDefault(candidate =>
                candidate.TelegramUserId.HasValue &&
                candidate.TelegramUserId == telegramUserId));
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => Task.FromResult(userToAdd);
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class DirectUserByIdRepository(User? user = null) : IUserRepository {
        public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByEmailIncludingDeletedAsync(string email, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByIdAsync(UserId id, CancellationToken cancellationToken = default) => Task.FromResult<User?>(user?.Id == id ? user : null);
        public Task<User?> GetByIdIncludingDeletedAsync(UserId id, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User?> GetByTelegramUserIdIncludingDeletedAsync(long telegramUserId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(IReadOnlyList<User> Items, int TotalItems)> GetPagedAsync(string? search, int page, int limit, bool includeDeleted, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<(int TotalUsers, int ActiveUsers, int PremiumUsers, int DeletedUsers, IReadOnlyList<User> RecentUsers)> GetAdminDashboardSummaryAsync(int recentLimit, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Role>> GetRolesByNamesAsync(IReadOnlyList<string> names, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<User> AddAsync(User userToAdd, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task UpdateAsync(User userToUpdate, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubPasswordHasher : IPasswordHasher {
        public string Hash(string password) => password;

        public bool Verify(string password, string hashedPassword) => string.Equals(password, hashedPassword, StringComparison.Ordinal);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(new(2030, 3, 28, 12, 0, 0, DateTimeKind.Utc));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubAuthenticationTokenService : IAuthenticationTokenService {
        public User? LastUser { get; private set; }

        public Task<IssuedAuthenticationTokens> IssueAndStoreAsync(
            User user,
            CancellationToken cancellationToken,
            AuthenticationClientContext? clientContext = null,
            bool rememberMe = false,
            Guid? refreshSessionId = null) {
            LastUser = user;
            return Task.FromResult(new IssuedAuthenticationTokens("access", "refresh"));
        }

        public string IssueAccessToken(User user) => throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubNotificationRepository(params Notification[] notifications) : INotificationRepository {
        public List<Notification> Notifications { get; } = [.. notifications];

        public Task<IReadOnlyList<Notification>> GetByUserAsync(UserId userId, int limit = 50, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Notification>>(Notifications.Where(x => x.UserId == userId).Take(limit).ToList());

        public Task<Notification?> GetByIdAsync(NotificationId id, bool asTracking = false, CancellationToken cancellationToken = default) =>
            Task.FromResult<Notification?>(Notifications.FirstOrDefault(x => x.Id == id));

        public Task<Notification> AddAsync(Notification notification, CancellationToken cancellationToken = default) {
            Notifications.Add(notification);
            return Task.FromResult(notification);
        }

        public Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<bool> ExistsAsync(UserId userId, string type, string referenceId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Any(x => x.UserId == userId && string.Equals(x.Type, type, StringComparison.Ordinal) && string.Equals(x.ReferenceId, referenceId, StringComparison.Ordinal)));

        public Task<int> GetUnreadCountAsync(UserId userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(x => x.UserId == userId && !x.IsRead));

        public Task<int> GetUnreadCountAsync(UserId userId, string type, CancellationToken cancellationToken = default) =>
            Task.FromResult(Notifications.Count(x => x.UserId == userId && !x.IsRead && string.Equals(x.Type, type, StringComparison.Ordinal)));

        public Task MarkAllReadAsync(UserId userId, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task<int> DeleteExpiredBatchAsync(
            IReadOnlyCollection<string> transientTypes,
            DateTime transientReadOlderThanUtc,
            DateTime transientUnreadOlderThanUtc,
            DateTime standardReadOlderThanUtc,
            DateTime standardUnreadOlderThanUtc,
            int batchSize,
            CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubEmailVerificationNotifier(bool throwOnNotify = false) : IEmailVerificationNotifier {
        public Guid? LastUserId { get; private set; }

        public Task NotifyEmailVerifiedAsync(Guid userId, CancellationToken cancellationToken = default) {
            if (throwOnNotify) {
                throw new InvalidOperationException("notification failed");
            }

            LastUserId = userId;
            return Task.CompletedTask;
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubEmailSender(bool throwOnEmailVerification = false, bool throwOnPasswordReset = false) : IEmailSender {
        public EmailVerificationMessage? LastEmailVerification { get; private set; }
        public PasswordResetMessage? LastPasswordReset { get; private set; }

        public Task SendEmailVerificationAsync(EmailVerificationMessage message, CancellationToken cancellationToken) {
            if (throwOnEmailVerification) {
                throw new InvalidOperationException("smtp failed");
            }

            LastEmailVerification = message;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(PasswordResetMessage message, CancellationToken cancellationToken) {
            if (throwOnPasswordReset) {
                throw new InvalidOperationException("smtp failed");
            }

            LastPasswordReset = message;
            return Task.CompletedTask;
        }

        public Task SendTestEmailAsync(TestEmailMessage message, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubTelegramAuthValidator(bool validateFailure = false) : ITelegramAuthValidator {
        public FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result<TelegramInitData> ValidateInitData(string initData) =>
            validateFailure
                ? FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result.Failure<TelegramInitData>(
                    Errors.Validation.Invalid("initData", "Invalid Telegram init data."))
                : FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result.Success(
                    new TelegramInitData(123456, "alex", "Alex", "User", PhotoUrl: null, "en", DateTime.UtcNow));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubTelegramLoginWidgetValidator(bool validateFailure = false) : ITelegramLoginWidgetValidator {
        public FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result<TelegramInitData> ValidateLoginWidget(TelegramLoginWidgetData data) =>
            validateFailure
                ? FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result.Failure<TelegramInitData>(
                    Errors.Validation.Invalid("hash", "Invalid Telegram login widget hash."))
                : FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result.Success(
                    new TelegramInitData(data.Id, data.Username, data.FirstName, data.LastName, data.PhotoUrl, LanguageCode: null, DateTime.UtcNow));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubGoogleTokenValidator(GoogleIdentityPayload payload, bool validateFailure = false) : IGoogleTokenValidator {
        public Task<FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result<GoogleIdentityPayload>> ValidateCredentialAsync(
            string credential,
            CancellationToken cancellationToken) =>
            Task.FromResult(validateFailure
                ? FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result.Failure<GoogleIdentityPayload>(
                    Errors.Validation.Invalid("credential", "Invalid Google credential."))
                : FoodDiary.Application.Abstractions.Common.Abstractions.Results.Result.Success(payload));
    }

    private static ResendEmailVerificationCommandHandler CreateResendEmailVerificationHandler(
        User user,
        StubEmailSender sender) =>
        new(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            sender,
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<ResendEmailVerificationCommandHandler>.Instance);
}
