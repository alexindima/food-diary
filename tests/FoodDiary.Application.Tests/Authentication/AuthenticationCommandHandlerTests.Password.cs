using FoodDiary.Application.Authentication.Commands.ConfirmPasswordReset;
using FoodDiary.Application.Authentication.Commands.RequestPasswordReset;
using FoodDiary.Application.Authentication.Commands.RestoreAccount;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.Application.Authentication.Models;
using FoodDiary.Application.Abstractions.Authentication.Common;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {

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

        ResultAssert.Success(result);
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

        ResultAssert.Success(result);
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

        ResultAssert.Success(result);
        Assert.NotNull(user.PasswordResetTokenHash);
        Assert.Equal(user.Email, sender.LastPasswordReset?.ToEmail);
        Assert.Equal("https://client.test", sender.LastPasswordReset?.ClientOrigin);
    }

    [Fact]
    public async Task RequestPasswordResetHandler_WhenEmailEnqueueFails_Throws() {
        var user = User.Create("reset-email-fails@example.com", "secret");
        var handler = new RequestPasswordResetCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubEmailSender(throwOnPasswordReset: true),
            new StubDateTimeProvider(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<RequestPasswordResetCommandHandler>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new RequestPasswordResetCommand(user.Email), CancellationToken.None));
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(),
            Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(Guid.Empty, "token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConfirmPasswordResetHandler_WhenUserMissing_ReturnsNotFound() {
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new StubAuthenticationTokenService(),
            Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(Guid.NewGuid(), "token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
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
            Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
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
            Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new NullAuditLogger());

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
        var handler = new ConfirmPasswordResetCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            dateTimeProvider,
            tokenService,
            Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "invalid-token", "StrongPass123"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.Null(tokenService.LastUser);
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
            Substitute.For<IRefreshTokenSessionWriteRepository>(),
            new NullAuditLogger());

        Result<AuthenticationModel> result = await handler.Handle(
            new ConfirmPasswordResetCommand(user.Id.Value, "token", "new-password"),
            CancellationToken.None);

        ResultAssert.Success(result);
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

        ResultAssert.Success(result);
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

        ResultAssert.Failure(result);
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

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountNotDeleted", result.Error.Code);
    }
}
