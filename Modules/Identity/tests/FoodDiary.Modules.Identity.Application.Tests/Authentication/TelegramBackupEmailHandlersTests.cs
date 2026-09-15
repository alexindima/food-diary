using FoodDiary.Mediator;
using FoodDiary.Testing;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramBackupEmail;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CompleteTelegramBackupEmail;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Services;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramBackupEmailHandlersTests {
    [Fact]
    public async Task DisabledProvider_DoesNotCreateOrConsumeAnAttempt() {
        _provider.IsEnabled.Returns(returnThis: false);
        Assert.Equal(TelegramIdentityErrors.NotConfigured.Code,
            (await Create().Send(new StartTelegramBackupEmailCommand(_user.Id.Value, "backup@example.com", _browser), CancellationToken.None)).Error.Code);
        Assert.Equal(TelegramIdentityErrors.InvalidProof.Code,
            (await Create().Send(new CompleteTelegramBackupEmailCommand(_user.Id.Value, "code", "state", _browser), CancellationToken.None)).Error.Code);
        Assert.Empty(_tickets.ReceivedCalls());
    }

    [Fact]
    public async Task Start_UnavailablePrincipalCannotCreateProof() {
        _identities.GetAuthenticationPrincipalAsync(_user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserAuthenticationPrincipalModel>(UserErrors.NotFound()));
        Assert.True((await Create().Send(new StartTelegramBackupEmailCommand(_user.Id.Value, "backup@example.com", _browser), CancellationToken.None)).IsFailure);
        Assert.Empty(_stored);
    }

    [Fact]
    public async Task Start_PreservesAuthorizationProviderFailure() {
        _provider.CreateAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(Result.Failure<string>(TelegramIdentityErrors.NotConfigured));
        Assert.Equal(TelegramIdentityErrors.NotConfigured.Code,
            (await Create().Send(new StartTelegramBackupEmailCommand(_user.Id.Value, "backup@example.com", _browser), CancellationToken.None)).Error.Code);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task Complete_RejectsCorruptOrForeignStoredAttempt(string payload) {
        _stored[("telegram-backup-email-oidc", $"{_browser}:{_user.Id.Value:D}")] = payload;
        Result result = await Create().Send(new CompleteTelegramBackupEmailCommand(_user.Id.Value, "code", "state", _browser), CancellationToken.None);
        Assert.Equal(TelegramIdentityErrors.InvalidProof.Code, result.Error.Code);
        await _provider.DidNotReceive().ExchangeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        Assert.Empty(_mail.ReceivedCalls());
    }

    [Fact]
    public async Task Complete_PropagatesProviderRejectionWithoutSendingEmail() {
        ISender service = Create();
        await service.Send(new StartTelegramBackupEmailCommand(_user.Id.Value, "backup@example.com", _browser), CancellationToken.None);
        _provider.ExchangeAsync("code", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.InvalidProof));
        Result result = await service.Send(new CompleteTelegramBackupEmailCommand(_user.Id.Value, "code", "state", _browser), CancellationToken.None);
        Assert.Equal(TelegramIdentityErrors.InvalidProof.Code, result.Error.Code);
        Assert.Empty(_mail.ReceivedCalls());
    }

    private readonly ITelegramOidcProvider _provider = Substitute.For<ITelegramOidcProvider>();
    private readonly ITelegramLoginTicketStore _tickets = Substitute.For<ITelegramLoginTicketStore>();
    private readonly IUserAuthenticationIdentityService _identities = Substitute.For<IUserAuthenticationIdentityService>();
    private readonly IEmailSender _mail = Substitute.For<IEmailSender>();
    private readonly User _user = User.CreateTelegram(123, "hash");
    private readonly string _browser = new('b', 43);
    private readonly Dictionary<(string Purpose, string Binding), string> _stored = [];

    public TelegramBackupEmailHandlersTests() {
        _provider.IsEnabled.Returns(returnThis: true);
        _provider.CreateAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Result.Success("https://oauth.telegram.org/auth"));
        _provider.ExchangeAsync("code", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TelegramOidcIdentity("https://oauth.telegram.org", "123", 123, FirstName: null, LastName: null, Username: null)));
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(_user, DateTime.UtcNow);
        _identities.GetAuthenticationPrincipalAsync(_user.Id, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        _identities.AuthenticateTelegramAsync(123, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        _tickets.CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => { _stored[(call.ArgAt<string>(0), call.ArgAt<string>(1))] = call.ArgAt<string>(2); return "state"; });
        _tickets.ConsumeAsync("state", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _stored.Remove((call.ArgAt<string>(1), call.ArgAt<string>(2)), out string? value) ? value : null);
    }

    [Fact]
    public async Task Complete_SendsOnlyBoundEmailAndConsumesAttemptOnce() {
        ISender service = Create();
        Assert.True((await service.Send(new StartTelegramBackupEmailCommand(_user.Id.Value, "Backup@example.com", _browser), CancellationToken.None)).IsSuccess);
        Assert.True((await service.Send(new CompleteTelegramBackupEmailCommand(_user.Id.Value, "code", "state", _browser), CancellationToken.None)).IsSuccess);
        Assert.True((await service.Send(new CompleteTelegramBackupEmailCommand(_user.Id.Value, "code", "state", _browser), CancellationToken.None)).IsFailure);
        await _mail.Received(1).SendEmailVerificationAsync(Arg.Is<EmailVerificationMessage>(m => m.ToEmail == "backup@example.com"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("browser")]
    [InlineData("account")]
    [InlineData("expired")]
    [InlineData("identity")]
    [InlineData("generation")]
    public async Task Complete_RejectsInvalidProofWithoutSending(string scenario) {
        ISender service = Create();
        await service.Send(new StartTelegramBackupEmailCommand(_user.Id.Value, "backup@example.com", _browser), CancellationToken.None);
        if (string.Equals(scenario, "expired", StringComparison.Ordinal)) { _stored.Clear(); }
        if (scenario is "identity" or "generation") {
            UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(_user, DateTime.UtcNow);
            principal = string.Equals(scenario, "identity", StringComparison.Ordinal) ? principal with { UserId = new UserId(Guid.NewGuid()) } : principal with { SecurityVersion = principal.SecurityVersion + 1 };
            _identities.AuthenticateTelegramAsync(123, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        }
        Result result = await service.Send(new CompleteTelegramBackupEmailCommand(string.Equals(scenario, "account", StringComparison.Ordinal) ? Guid.NewGuid() : _user.Id.Value, "code", "state", string.Equals(scenario, "browser", StringComparison.Ordinal) ? new string('x', 43) : _browser), CancellationToken.None);
        Assert.True(result.IsFailure);
        await _mail.DidNotReceive().SendEmailVerificationAsync(Arg.Any<EmailVerificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_RejectsInvalidEmailBeforeProviderOrTicketUse() {
        Assert.True((await Create().Send(new StartTelegramBackupEmailCommand(_user.Id.Value, "invalid", _browser), CancellationToken.None)).IsFailure);
        Assert.Empty(_stored);
        _provider.DidNotReceive().CreateAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    private ISender Create() {
        var backup = new TelegramBackupEmailService(Substitute.For<ITelegramAuthValidator>(), Substitute.For<ITelegramAssertionReplayGuard>(),
            _identities, Substitute.For<IUserTelegramAccountService>(), _tickets, _mail, TimeProvider.System);
        return RequestTestSender.Create(
            new StartTelegramBackupEmailCommandHandler(_provider, _tickets, _identities, TimeProvider.System),
            new CompleteTelegramBackupEmailCommandHandler(_provider, _tickets, backup));
    }
}
