using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Text.Json;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.VerifyEmail;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramBackupEmailServiceTests {
    private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
    private readonly ITelegramAuthValidator _validator = Substitute.For<ITelegramAuthValidator>();
    private readonly ITelegramAssertionReplayGuard _replay = Substitute.For<ITelegramAssertionReplayGuard>();
    private readonly IUserAuthenticationIdentityService _identities = Substitute.For<IUserAuthenticationIdentityService>();
    private readonly IUserTelegramAccountService _accounts = Substitute.For<IUserTelegramAccountService>();
    private readonly ITelegramLoginTicketStore _tickets = Substitute.For<ITelegramLoginTicketStore>();
    private readonly IEmailSender _mail = Substitute.For<IEmailSender>();

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("Name <mail@example.com>")]
    public async Task Request_InvalidEmailDoesNotValidateProofOrSendMail(string email) {
        Result result = await Create().RequestAsync(Guid.NewGuid(), email, "proof", CancellationToken.None);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(_validator.ReceivedCalls());
        Assert.Empty(_mail.ReceivedCalls());
    }

    [Theory]
    [InlineData("invalid-signature")]
    [InlineData("expired")]
    [InlineData("future")]
    [InlineData("unspecified-time")]
    [InlineData("missing-user")]
    [InlineData("other-user")]
    [InlineData("email-present")]
    [InlineData("replayed")]
    public async Task Request_RejectsUntrustedProofBeforeCreatingEmailTicket(string scenario) {
        var user = User.CreateTelegram(123, "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, Now);
        DateTime proofTime = scenario switch {
            "expired" => Now.AddMinutes(-5),
            "future" => Now.AddSeconds(1),
            "unspecified-time" => DateTime.SpecifyKind(Now, DateTimeKind.Unspecified),
            _ => Now,
        };
        if (string.Equals(scenario, "email-present", StringComparison.Ordinal)) {
            principal = principal with { Email = "existing@example.com" };
        }
        _validator.ValidateInitData("proof").Returns(string.Equals(scenario, "invalid-signature", StringComparison.Ordinal)
            ? Result.Failure<TelegramInitData>(TelegramIdentityErrors.InvalidProof)
            : Result.Success(new TelegramInitData(123, Username: null, FirstName: null, LastName: null, PhotoUrl: null, LanguageCode: null, proofTime)));
        _identities.AuthenticateTelegramAsync(123, Now, Arg.Any<CancellationToken>()).Returns(string.Equals(scenario, "missing-user", StringComparison.Ordinal)
            ? Result.Failure<UserAuthenticationPrincipalModel>(UserErrors.NotFound()) : Result.Success(principal));
        _replay.TryConsumeAsync("proof", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(returnThis: false);

        Result result = await Create().RequestAsync(string.Equals(scenario, "other-user", StringComparison.Ordinal) ? Guid.NewGuid() : user.Id.Value,
            "backup@example.com", "proof", CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Empty(_tickets.ReceivedCalls());
        Assert.Empty(_mail.ReceivedCalls());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    public async Task Confirm_MalformedTicketCannotAssignEmail(string payload) {
        var userId = Guid.NewGuid();
        _tickets.ConsumeAsync("ticket", "telegram-backup-email", userId.ToString("D"), Arg.Any<CancellationToken>()).Returns(payload);
        Result result = await Create().ConfirmAsync(userId, "telegram-backup.ticket", CancellationToken.None);
        Assert.Equal("Authentication.TelegramProofRequired", result.Error.Code);
        Assert.Empty(_accounts.ReceivedCalls());
    }

    [Fact]
    public async Task Confirm_WrongTokenPurposeIsRejectedBeforeConsumption() {
        Result result = await Create().ConfirmAsync(Guid.NewGuid(), "ordinary-email-token", CancellationToken.None);
        Assert.Equal("Authentication.TelegramProofRequired", result.Error.Code);
        Assert.Empty(_tickets.ReceivedCalls());
    }

    [Fact]
    public async Task Request_SendsBoundProofWithoutAssigningEmail() {
        var user = User.CreateTelegram(123, "hash");
        _validator.ValidateInitData("proof").Returns(Result.Success(new TelegramInitData(123, Username: null,
            FirstName: null, LastName: null, PhotoUrl: null, "ru", Now)));
        _identities.AuthenticateTelegramAsync(123, Now, Arg.Any<CancellationToken>())
            .Returns(Result.Success(UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, Now)));
        _replay.TryConsumeAsync("proof", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        _tickets.CreateAsync("telegram-backup-email", user.Id.Value.ToString("D"), Arg.Any<string>(), Now.AddMinutes(15), Arg.Any<CancellationToken>()).Returns("ticket");

        Result result = await Create().RequestAsync(user.Id.Value, "Backup@example.com", "proof", CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _mail.Received(1).SendEmailVerificationAsync(Arg.Is<EmailVerificationMessage>(message =>
            message.Token == "telegram-backup.ticket" && message.UserId == user.Id.Value.ToString("D")), Arg.Any<CancellationToken>());
        await _accounts.DidNotReceive().AddVerifiedEmailAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Confirm_UsesTicketEmailAndGenerationAndRejectsOtherUser(bool foreignOwner) {
        var userId = Guid.NewGuid();
        string payload = JsonSerializer.Serialize(new { UserId = foreignOwner ? Guid.NewGuid() : userId, Email = "backup@example.com", SecurityVersion = 7 });
        _tickets.ConsumeAsync("ticket", "telegram-backup-email", userId.ToString("D"), Arg.Any<CancellationToken>()).Returns(payload);
        _accounts.AddVerifiedEmailAsync(new UserId(userId), "backup@example.com", 7, Arg.Any<CancellationToken>()).Returns(Result.Success());

        Result result = await Create().ConfirmAsync(userId, "telegram-backup.ticket", CancellationToken.None);

        Assert.Equal(!foreignOwner, result.IsSuccess);
        await _accounts.Received(foreignOwner ? 0 : 1).AddVerifiedEmailAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Confirm_ExpiredOrUsedTicketDoesNotChangeAccount() {
        _tickets.ConsumeAsync("expired", "telegram-backup-email", Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((string?)null);
        Result result = await Create().ConfirmAsync(Guid.NewGuid(), "telegram-backup.expired", CancellationToken.None);
        Assert.True(result.IsFailure);
        await _accounts.DidNotReceive().AddVerifiedEmailAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExistingVerificationEntryPoint_DispatchesBackupProofToItsOwnPurpose() {
        var userId = Guid.NewGuid();
        string payload = JsonSerializer.Serialize(new { UserId = userId, Email = "backup@example.com", SecurityVersion = 7 });
        _tickets.ConsumeAsync("ticket", "telegram-backup-email", userId.ToString("D"), Arg.Any<CancellationToken>()).Returns(payload);
        _accounts.AddVerifiedEmailAsync(new UserId(userId), "backup@example.com", 7, Arg.Any<CancellationToken>()).Returns(Result.Success());
        var handler = new VerifyEmailCommandHandler(_identities, new Clock(), Substitute.For<IPostCommitActionQueue>(),
            Substitute.For<IEmailVerificationNotifier>(), Create());

        Result result = await handler.Handle(new VerifyEmailCommand(userId, "telegram-backup.ticket"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        await _accounts.Received(1).AddVerifiedEmailAsync(new UserId(userId), "backup@example.com", 7, Arg.Any<CancellationToken>());
        await _identities.DidNotReceive().VerifyEmailAsync(Arg.Any<UserId>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
    }

    private TelegramBackupEmailService Create() => new(_validator, _replay, _identities, _accounts, _tickets, _mail, new Clock());
    [ExcludeFromCodeCoverage]
    private sealed class Clock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }
}
