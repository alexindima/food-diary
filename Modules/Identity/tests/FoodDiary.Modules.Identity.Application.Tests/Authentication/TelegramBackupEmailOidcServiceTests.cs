using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramBackupEmailOidcServiceTests {
    private readonly ITelegramOidcProvider _provider = Substitute.For<ITelegramOidcProvider>();
    private readonly ITelegramLoginTicketStore _tickets = Substitute.For<ITelegramLoginTicketStore>();
    private readonly IUserAuthenticationIdentityService _identities = Substitute.For<IUserAuthenticationIdentityService>();
    private readonly IEmailSender _mail = Substitute.For<IEmailSender>();
    private readonly User _user = User.CreateTelegram(123, "hash");
    private readonly string _browser = new('b', 43);
    private readonly Dictionary<(string Purpose, string Binding), string> _stored = [];

    public TelegramBackupEmailOidcServiceTests() {
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
        TelegramBackupEmailOidcService service = Create();
        Assert.True((await service.StartAsync(_user.Id.Value, "Backup@example.com", _browser, CancellationToken.None)).IsSuccess);
        Assert.True((await service.CompleteAsync(_user.Id.Value, "code", "state", _browser, CancellationToken.None)).IsSuccess);
        Assert.True((await service.CompleteAsync(_user.Id.Value, "code", "state", _browser, CancellationToken.None)).IsFailure);
        await _mail.Received(1).SendEmailVerificationAsync(Arg.Is<EmailVerificationMessage>(m => m.ToEmail == "backup@example.com"), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("browser")]
    [InlineData("account")]
    [InlineData("expired")]
    [InlineData("identity")]
    [InlineData("generation")]
    public async Task Complete_RejectsInvalidProofWithoutSending(string scenario) {
        TelegramBackupEmailOidcService service = Create();
        await service.StartAsync(_user.Id.Value, "backup@example.com", _browser, CancellationToken.None);
        if (string.Equals(scenario, "expired", StringComparison.Ordinal)) { _stored.Clear(); }
        if (scenario is "identity" or "generation") {
            UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(_user, DateTime.UtcNow);
            principal = string.Equals(scenario, "identity", StringComparison.Ordinal) ? principal with { UserId = new UserId(Guid.NewGuid()) } : principal with { SecurityVersion = principal.SecurityVersion + 1 };
            _identities.AuthenticateTelegramAsync(123, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        }
        Result result = await service.CompleteAsync(string.Equals(scenario, "account", StringComparison.Ordinal) ? Guid.NewGuid() : _user.Id.Value,
            "code", "state", string.Equals(scenario, "browser", StringComparison.Ordinal) ? new string('x', 43) : _browser, CancellationToken.None);
        Assert.True(result.IsFailure);
        await _mail.DidNotReceive().SendEmailVerificationAsync(Arg.Any<EmailVerificationMessage>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Start_RejectsInvalidEmailBeforeProviderOrTicketUse() {
        Assert.True((await Create().StartAsync(_user.Id.Value, "invalid", _browser, CancellationToken.None)).IsFailure);
        Assert.Empty(_stored);
        _provider.DidNotReceive().CreateAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    private TelegramBackupEmailOidcService Create() {
        var backup = new TelegramBackupEmailService(Substitute.For<ITelegramAuthValidator>(), Substitute.For<ITelegramAssertionReplayGuard>(),
            _identities, Substitute.For<IUserTelegramAccountService>(), _tickets, _mail, TimeProvider.System);
        return new TelegramBackupEmailOidcService(_provider, _tickets, _identities, backup, TimeProvider.System);
    }
}
