using FoodDiary.Application.Identity.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Identity.Authentication.Commands.RequestPasswordReset;
using FoodDiary.Application.Identity.Authentication.Commands.RestoreAccount;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Primitives;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    private static RequestPasswordResetCommandHandler CreateRequestPasswordResetHandler(
        StubUserRepository userRepository,
        StubEmailSender sender,
        StubDateTimeProvider? dateTimeProvider = null) =>
        new(
            CreateUserAuthenticationIdentityService(userRepository),
            sender,
            dateTimeProvider ?? new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RequestPasswordResetCommandHandler>.Instance);

    private static ConfirmPasswordResetCommandHandler CreateConfirmPasswordResetHandler(
        StubUserRepository userRepository,
        StubDateTimeProvider? dateTimeProvider = null,
        StubAuthenticationTokenService? tokenService = null,
        IRefreshTokenSessionWriteRepository? refreshTokenSessionRepository = null) =>
        new(
            CreateUserAuthenticationIdentityService(userRepository),
            dateTimeProvider ?? new StubDateTimeProvider(),
            tokenService ?? new StubAuthenticationTokenService(),
            refreshTokenSessionRepository ?? Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new NullAuditLogger());

    [Fact]
    public async Task RequestPasswordResetHandler_WhenUserMissing_ReturnsSuccessWithoutSending() {
        var sender = new StubEmailSender();
        RequestPasswordResetCommandHandler handler = CreateRequestPasswordResetHandler(new StubUserRepository(), sender);

        Result result = await handler.Handle(new RequestPasswordResetCommand("missing@example.com"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(sender.LastPasswordReset);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WhenRequestIsInCooldown_ReturnsSuccessWithoutSending() {
        var user = User.Create("cooldown-reset@example.com", "secret");
        DateTime nowUtc = new StubDateTimeProvider().GetUtcNow().UtcDateTime;
        user.SetPasswordResetToken(new UserTokenIssue("old-hash", nowUtc.AddHours(1), nowUtc.AddSeconds(-30)));
        var sender = new StubEmailSender();
        RequestPasswordResetCommandHandler handler = CreateRequestPasswordResetHandler(new StubUserRepository(user), sender);

        Result result = await handler.Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(sender.LastPasswordReset);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WithActiveUser_UpdatesTokenAndSendsMessage() {
        var user = User.Create("reset@example.com", "secret");
        var sender = new StubEmailSender();
        RequestPasswordResetCommandHandler handler = CreateRequestPasswordResetHandler(new StubUserRepository(user), sender);

        Result result = await handler.Handle(
            new RequestPasswordResetCommand(user.Email, "https://client.test"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(user.PasswordResetTokenHash);
        Assert.Equal(user.Email, sender.LastPasswordReset?.ToEmail);
        Assert.Equal("https://client.test", sender.LastPasswordReset?.ClientOrigin);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WhenEmailEnqueueFails_Throws() {
        var user = User.Create("reset-email-fails@example.com", "secret");
        RequestPasswordResetCommandHandler handler = CreateRequestPasswordResetHandler(
            new StubUserRepository(user),
            new StubEmailSender(throwOnPasswordReset: true));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None));
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WhenUserInactive_ReturnsSuccessWithoutSending() {
        var user = User.Create("inactive-reset@example.com", "secret");
        user.Deactivate();
        var sender = new StubEmailSender();
        RequestPasswordResetCommandHandler handler = CreateRequestPasswordResetHandler(new StubUserRepository(user), sender);

        Result result = await handler.Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(sender.LastPasswordReset);
        Assert.Null(user.PasswordResetTokenHash);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WhenUserDeleted_ReturnsSuccessWithoutSending() {
        var user = User.Create("deleted-reset@example.com", "secret");
        user.DeleteAccount(DateTime.UtcNow);
        var sender = new StubEmailSender();
        RequestPasswordResetCommandHandler handler = CreateRequestPasswordResetHandler(new StubUserRepository(user), sender);

        Result result = await handler.Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(sender.LastPasswordReset);
        Assert.Null(user.PasswordResetTokenHash);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WithEmptyUserId_ReturnsValidationFailure() {
        ConfirmPasswordResetCommandHandler handler = CreateConfirmPasswordResetHandler(new StubUserRepository());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(Guid.Empty, "token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenUserMissing_ReturnsNotFound() {
        ConfirmPasswordResetCommandHandler handler = CreateConfirmPasswordResetHandler(new StubUserRepository());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(Guid.NewGuid(), "token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenTokenMissing_ReturnsInvalidToken() {
        var user = User.Create("reset-missing-token@example.com", "secret");
        ConfirmPasswordResetCommandHandler handler = CreateConfirmPasswordResetHandler(new StubUserRepository(user));

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenTokenExpired_ReturnsInvalidToken() {
        var user = User.Create("reset-expired-token@example.com", "secret");
        var dateTimeProvider = new StubDateTimeProvider();
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        using (DomainTime.Override(new FixedDomainTimeProvider(nowUtc.AddHours(-1)))) {
            user.SetPasswordResetToken(new UserTokenIssue("token", nowUtc.AddMinutes(-1), nowUtc.AddHours(-1)));
        }
        var tokenService = new StubAuthenticationTokenService();
        ConfirmPasswordResetCommandHandler handler = CreateConfirmPasswordResetHandler(
            new StubUserRepository(user),
            dateTimeProvider,
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(tokenService.LastPrincipal);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenTokenExpiresExactlyNow_ReturnsInvalidToken() {
        var user = User.Create("reset-boundary-token@example.com", "secret");
        var dateTimeProvider = new StubDateTimeProvider();
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        using (DomainTime.Override(new FixedDomainTimeProvider(nowUtc.AddHours(-1)))) {
            user.SetPasswordResetToken(new UserTokenIssue("token", nowUtc, nowUtc.AddHours(-1)));
        }
        var tokenService = new StubAuthenticationTokenService();
        ConfirmPasswordResetCommandHandler handler = CreateConfirmPasswordResetHandler(
            new StubUserRepository(user),
            dateTimeProvider,
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(tokenService.LastPrincipal);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenTokenDoesNotMatch_ReturnsInvalidToken() {
        var user = User.Create("reset-bad-token@example.com", "secret");
        DateTime issuedAtUtc = DateTime.UtcNow;
        using (DomainTime.Override(new FixedDomainTimeProvider(issuedAtUtc))) {
            user.SetPasswordResetToken(new UserTokenIssue("expected", issuedAtUtc.AddHours(1), issuedAtUtc));
        }
        ConfirmPasswordResetCommandHandler handler = CreateConfirmPasswordResetHandler(new StubUserRepository(user));

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "actual", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenVerifierRejectsToken_ReturnsInvalidToken() {
        var user = User.Create("reset-rejected-token@example.com", "secret");
        var dateTimeProvider = new StubDateTimeProvider();
        user.SetPasswordResetToken(new UserTokenIssue("valid-token", dateTimeProvider.GetUtcNow().UtcDateTime.AddHours(1), dateTimeProvider.GetUtcNow().UtcDateTime));
        var tokenService = new StubAuthenticationTokenService();
        ConfirmPasswordResetCommandHandler handler = CreateConfirmPasswordResetHandler(
            new StubUserRepository(user),
            dateTimeProvider,
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "invalid-token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(tokenService.LastUser);
        Assert.Null(tokenService.LastPrincipal);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WithValidToken_ChangesPasswordAndIssuesTokens() {
        var user = User.Create("reset-valid@example.com", "old-password");
        var dateTimeProvider = new StubDateTimeProvider();
        user.SetPasswordResetToken(new UserTokenIssue("token", dateTimeProvider.GetUtcNow().UtcDateTime.AddHours(1), dateTimeProvider.GetUtcNow().UtcDateTime));
        var tokenService = new StubAuthenticationTokenService();
        ConfirmPasswordResetCommandHandler handler = CreateConfirmPasswordResetHandler(
            new StubUserRepository(user),
            dateTimeProvider,
            tokenService);

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "token", "new-password"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal("access", result.Value.AccessToken);
        Assert.Null(tokenService.LastUser);
        Assert.Equal(user.Id, tokenService.LastPrincipal?.UserId);
        Assert.Equal(user.Email, tokenService.LastPrincipal?.Email);
    }

    [Fact]
    public async Task RestoreAccountHandler_WithDeletedUser_RestoresAndIssuesTokens() {
        var user = User.Create("deleted@example.com", "secret");
        user.DeleteAccount(DateTime.UtcNow.AddDays(-2));
        var tokenService = new StubAuthenticationTokenService();
        IRefreshTokenSessionWriteRepository refreshSessions = Substitute.For<IRefreshTokenSessionWriteRepository>();
        var dateTimeProvider = new StubDateTimeProvider();
        var handler = new RestoreAccountCommandHandler(
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            dateTimeProvider,
            refreshSessions,
            tokenService);
        var clientContext = new AuthenticationClientContext("password", "203.0.113.11", "test-agent");

        Result<AuthenticationModel> result = await handler.Handle(
            new RestoreAccountCommand(user.Email, "secret", RememberMe: true, ClientContext: clientContext),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(user.IsActive);
        Assert.Null(user.DeletedAt);
        Assert.Equal(new StubDateTimeProvider().GetUtcNow().UtcDateTime, user.LastLoginAtUtc);
        Assert.Null(tokenService.LastUser);
        Assert.Equal(user.Id, tokenService.LastPrincipal?.UserId);
        Assert.Same(clientContext, tokenService.LastClientContext);
        Assert.True(tokenService.LastRememberMe);
        await refreshSessions.Received(1).RevokeAllAsync(
            user.Id,
            dateTimeProvider.GetUtcNow().UtcDateTime,
            CancellationToken.None);
    }

    [Fact]
    public async Task RestoreAccountHandler_WithMissingUser_ReturnsInvalidCredentials() {
        var handler = new RestoreAccountCommandHandler(
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
            new StubDateTimeProvider(),
            Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(
            new RestoreAccountCommand("missing@example.com", "secret"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidCredentials", result.Error.Code);
    }

    [Fact]
    public async Task RestoreAccountHandler_WithActiveUser_ReturnsAccountNotDeleted() {
        var user = User.Create("active-restore@example.com", "secret");
        var handler = new RestoreAccountCommandHandler(
            CreateUserAuthenticationIdentityService(new StubUserRepository(user)),
            new StubDateTimeProvider(),
            Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new StubAuthenticationTokenService());

        Result<AuthenticationModel> result = await handler.Handle(
            new RestoreAccountCommand(user.Email, "secret"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountNotDeleted", result.Error.Code);
    }
}
