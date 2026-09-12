using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Application.Identity.Authentication.Commands.VerifyEmail;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramBackupEmailServiceTests {
    private static readonly DateTime Now = new(2026, 9, 12, 12, 0, 0, DateTimeKind.Utc);
    private readonly ITelegramAuthValidator _validator = Substitute.For<ITelegramAuthValidator>();
    private readonly ITelegramAssertionReplayGuard _replay = Substitute.For<ITelegramAssertionReplayGuard>();
    private readonly IUserAuthenticationIdentityService _identities = Substitute.For<IUserAuthenticationIdentityService>();
    private readonly IUserTelegramAccountService _accounts = Substitute.For<IUserTelegramAccountService>();
    private readonly ITelegramLoginTicketStore _tickets = Substitute.For<ITelegramLoginTicketStore>();
    private readonly IEmailSender _mail = Substitute.For<IEmailSender>();

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
