using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Authentication.Commands.VerifyEmail;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {

    [Fact]
    public async Task VerifyEmailHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new ImmediatePostCommitActionQueue(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(Guid.Empty, "token"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenUserMissing_ReturnsNotFound() {
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new ImmediatePostCommitActionQueue(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(Guid.NewGuid(), "token"),
            CancellationToken.None);

        ResultAssert.Failure(result);
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
            new ImmediatePostCommitActionQueue(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        ResultAssert.Success(result);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenTokenMissing_ReturnsInvalidToken() {
        var user = User.Create("missing-token@example.com", "secret");
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            new StubDateTimeProvider(),
            new ImmediatePostCommitActionQueue(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        ResultAssert.Failure(result);
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
            new ImmediatePostCommitActionQueue(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "actual"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenVerifierRejectsToken_ReturnsInvalidToken() {
        var user = User.Create("verify-rejected-token@example.com", "secret");
        var dateTimeProvider = new StubDateTimeProvider();
        user.SetEmailConfirmationToken(new UserTokenIssue("valid-token", dateTimeProvider.GetUtcNow().UtcDateTime.AddHours(1), dateTimeProvider.GetUtcNow().UtcDateTime));
        var handler = new VerifyEmailCommandHandler(
            new StubUserRepository(user),
            new StubPasswordHasher(),
            dateTimeProvider,
            new ImmediatePostCommitActionQueue(),
            new StubEmailVerificationNotifier());

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "invalid-token"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(user.IsEmailConfirmed);
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
            new ImmediatePostCommitActionQueue(),
            notifier);

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        ResultAssert.Success(result);
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
            new ImmediatePostCommitActionQueue(),
            new StubEmailVerificationNotifier(throwOnNotify: true));

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.True(user.IsEmailConfirmed);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WithEmptyUserId_ReturnsValidationFailure() {
        var handler = new ResendEmailVerificationCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubEmailSender(),
            new StubDateTimeProvider());

        Result result = await handler.Handle(
            new ResendEmailVerificationCommand(Guid.Empty),
            CancellationToken.None);

        ResultAssert.Failure(result);
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

        ResultAssert.Success(result);
        Assert.Null(sender.LastEmailVerification);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WhenSentRecently_ReturnsCooldownFailure() {
        var user = User.Create("cooldown@example.com", "secret");
        user.SetEmailConfirmationToken(new UserTokenIssue("old-hash", DateTime.UtcNow.AddHours(1), new StubDateTimeProvider().GetUtcNow().UtcDateTime.AddSeconds(-30)));
        ResendEmailVerificationCommandHandler handler = CreateResendEmailVerificationHandler(user, new StubEmailSender());

        Result result = await handler.Handle(new ResendEmailVerificationCommand(user.Id.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("recently", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WithMissingUser_ReturnsInvalidToken() {
        var handler = new ResendEmailVerificationCommandHandler(
            new StubUserRepository(),
            new StubPasswordHasher(),
            new StubEmailSender(),
            new StubDateTimeProvider());

        Result result = await handler.Handle(new ResendEmailVerificationCommand(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
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

        ResultAssert.Success(result);
        Assert.NotNull(sender.LastEmailVerification);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WithActiveUnconfirmedUser_UpdatesTokenAndSendsMessage() {
        var user = User.Create("resend@example.com", "secret");
        var sender = new StubEmailSender();
        ResendEmailVerificationCommandHandler handler = CreateResendEmailVerificationHandler(user, sender);

        Result result = await handler.Handle(new ResendEmailVerificationCommand(user.Id.Value, "https://client.test"), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(user.EmailConfirmationTokenHash);
        Assert.NotNull(sender.LastEmailVerification);
        Assert.Equal("resend@example.com", sender.LastEmailVerification.ToEmail);
        Assert.Equal("https://client.test", sender.LastEmailVerification.ClientOrigin);
    }

    [Fact]
    public async Task ResendEmailVerificationHandler_WhenEmailEnqueueFails_Throws() {
        var user = User.Create("send-fails@example.com", "secret");
        ResendEmailVerificationCommandHandler handler = CreateResendEmailVerificationHandler(user, new StubEmailSender(throwOnEmailVerification: true));

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new ResendEmailVerificationCommand(user.Id.Value), CancellationToken.None));
    }
}
