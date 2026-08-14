using FoodDiary.Application.Identity.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Identity.Authentication.Commands.VerifyEmail;
using FoodDiary.Results;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Tests.Authentication;

public sealed partial class AuthenticationCommandHandlerTests {
    private static VerifyEmailCommandHandler CreateVerifyEmailHandler(
        StubUserRepository userRepository,
        StubDateTimeProvider? dateTimeProvider = null,
        StubEmailVerificationNotifier? notifier = null) =>
        new(
            CreateUserAuthenticationIdentityService(userRepository),
            dateTimeProvider ?? new StubDateTimeProvider(),
            new ImmediatePostCommitActionQueue(),
            notifier ?? new StubEmailVerificationNotifier());

    [Fact]
    public async Task VerifyEmailHandler_WithEmptyUserId_ReturnsValidationFailure() {
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(new StubUserRepository());

        Result result = await handler.Handle(
            new VerifyEmailCommand(Guid.Empty, "token"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("UserId", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenUserMissing_ReturnsNotFound() {
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(new StubUserRepository());

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
        var notifier = new StubEmailVerificationNotifier();
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(
            new StubUserRepository(user),
            notifier: notifier);

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(notifier.LastUserId);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenTokenMissing_ReturnsInvalidToken() {
        var user = User.Create("missing-token@example.com", "secret");
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(new StubUserRepository(user));

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenTokenExpired_ReturnsInvalidToken() {
        var user = User.Create("expired-token@example.com", "secret");
        var dateTimeProvider = new StubDateTimeProvider();
        DateTime nowUtc = dateTimeProvider.GetUtcNow().UtcDateTime;
        user.SetEmailConfirmationToken(new UserTokenIssue("token", nowUtc.AddMinutes(-1), nowUtc.AddHours(-1)));
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(new StubUserRepository(user), dateTimeProvider);

        Result result = await handler.Handle(
            new VerifyEmailCommand(user.Id.Value, "token"),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
        Assert.False(user.IsEmailConfirmed);
    }

    [Fact]
    public async Task VerifyEmailHandler_WhenTokenDoesNotMatch_ReturnsInvalidToken() {
        var user = User.Create("bad-token@example.com", "secret");
        user.SetEmailConfirmationToken(new UserTokenIssue("expected", DateTime.UtcNow.AddHours(1), DateTime.UtcNow));
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(new StubUserRepository(user));

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
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(new StubUserRepository(user), dateTimeProvider);

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
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(new StubUserRepository(user), dateTimeProvider, notifier);

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
        VerifyEmailCommandHandler handler = CreateVerifyEmailHandler(
            new StubUserRepository(user),
            dateTimeProvider,
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
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
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
            CreateUserAuthenticationIdentityService(new StubUserRepository()),
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
