using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.CompleteTelegramUnlink;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.StartTelegramUnlink;
using FoodDiary.Modules.Users.Application.Services;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;
using FoodDiary.Testing;

namespace FoodDiary.Modules.Identity.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramUnlinkHandlersTests {
    private readonly ITelegramOidcProvider _provider = Substitute.For<ITelegramOidcProvider>();
    private readonly ITelegramLoginTicketStore _tickets = Substitute.For<ITelegramLoginTicketStore>();
    private readonly IUserAuthenticationIdentityService _identities = Substitute.For<IUserAuthenticationIdentityService>();
    private readonly IUserTelegramAccountService _accounts = Substitute.For<IUserTelegramAccountService>();
    private readonly ITelegramOperationStore _operations = Substitute.For<ITelegramOperationStore>();
    private readonly IPostCommitActionQueue _postCommit = Substitute.For<IPostCommitActionQueue>();
    private readonly User _user = User.CreateTelegram(123, "hash");
    private readonly string _browser = new('b', 43);
    private readonly Dictionary<(string Purpose, string Binding), string> _stored = [];
    private readonly DateTime _now = new(2026, 11, 3, 12, 0, 0, DateTimeKind.Utc);
    private Func<CancellationToken, Task>? _afterCommit;

    public TelegramUnlinkHandlersTests() {
        _provider.IsEnabled.Returns(returnThis: true);
        _provider.CreateAuthorizationUrl(Arg.Any<TelegramOidcAuthorizationRequest>()).Returns(Result.Success("https://oauth.telegram.org/auth"));
        _provider.ExchangeAsync(Arg.Any<TelegramOidcTokenExchange>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success(new TelegramOidcIdentity("https://oauth.telegram.org", "oidc-subject", 123, FirstName: null, LastName: null, Username: null)));
        UserAuthenticationPrincipalModel principal = Principal();
        _identities.GetAuthenticationPrincipalAsync(_user.Id, _now, Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        _identities.AuthenticateTelegramAsync(123, _now, Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        _accounts.UnlinkAsync(_user.Id, 123, principal.SecurityVersion, Arg.Any<CancellationToken>()).Returns(Result.Success());
        _tickets.CreateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(call => { _stored[(call.ArgAt<string>(0), call.ArgAt<string>(1))] = call.ArgAt<string>(2); return "state"; });
        _tickets.ConsumeAsync("state", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => _stored.Remove((call.ArgAt<string>(1), call.ArgAt<string>(2)), out string? value) ? value : null);
        _postCommit.When(queue => queue.Enqueue(Arg.Any<string>(), Arg.Any<Func<CancellationToken, Task>>()))
            .Do(call => _afterCommit = call.ArgAt<Func<CancellationToken, Task>>(1));
    }

    [Fact]
    public async Task FreshOidcProof_UnlinksOnceAndCancelsOperationsOnlyAfterCommit() {
        ISender sender = Create();
        Assert.True((await sender.Send(new StartTelegramUnlinkCommand(_user.Id.Value, _browser), CancellationToken.None)).IsSuccess);
        await _tickets.Received(1).CreateAsync("telegram-unlink-oidc", $"{_browser}:{_user.Id.Value:D}", Arg.Any<string>(), _now.AddMinutes(5), CancellationToken.None);
        TelegramOidcAuthorizationRequest request = _provider.ReceivedCalls().Single(call => string.Equals(call.GetMethodInfo().Name, "CreateAuthorizationUrl", StringComparison.Ordinal))
            .GetArguments().OfType<TelegramOidcAuthorizationRequest>().Single();

        Assert.True((await sender.Send(Complete(), CancellationToken.None)).IsSuccess);
        Assert.True((await sender.Send(Complete(), CancellationToken.None)).IsFailure);

        await _provider.Received(1).ExchangeAsync(Arg.Is<TelegramOidcTokenExchange>(exchange =>
            exchange.Code.Value == "code" && exchange.CodeVerifier == request.CodeVerifier && exchange.ExpectedNonce == request.Nonce), CancellationToken.None);
        await _accounts.Received(1).UnlinkAsync(_user.Id, 123, Principal().SecurityVersion, CancellationToken.None);
        Assert.Empty(_operations.ReceivedCalls());
        Assert.NotNull(_afterCommit);
        await _afterCommit(CancellationToken.None);
        await _operations.Received(1).CancelUserAsync(_user.Id, CancellationToken.None);
    }

    [Theory]
    [InlineData("browser")]
    [InlineData("account")]
    [InlineData("expired")]
    [InlineData("state")]
    [InlineData("identity")]
    [InlineData("generation")]
    [InlineData("provider")]
    [InlineData("last-login")]
    public async Task InvalidConfirmation_CannotDisconnectOrCancelOperations(string scenario) {
        ISender sender = Create();
        await sender.Send(new StartTelegramUnlinkCommand(_user.Id.Value, _browser), CancellationToken.None);
        if (string.Equals(scenario, "expired", StringComparison.Ordinal)) { _stored.Clear(); }
        if (scenario is "identity" or "generation") {
            UserAuthenticationPrincipalModel principal = Principal();
            principal = string.Equals(scenario, "identity", StringComparison.Ordinal) ? principal with { UserId = new UserId(Guid.NewGuid()) } : principal with { SecurityVersion = principal.SecurityVersion + 1 };
            _identities.AuthenticateTelegramAsync(123, _now, Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        }
        if (string.Equals(scenario, "provider", StringComparison.Ordinal)) {
            _provider.ExchangeAsync(Arg.Any<TelegramOidcTokenExchange>(), Arg.Any<CancellationToken>())
                .Returns(Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.InvalidProof));
        }
        if (string.Equals(scenario, "last-login", StringComparison.Ordinal)) {
            _accounts.UnlinkAsync(_user.Id, 123, Principal().SecurityVersion, Arg.Any<CancellationToken>())
                .Returns(Result.Failure(UserErrors.LastSignInMethod));
        }
        Result result = await sender.Send(Complete() with {
            UserId = string.Equals(scenario, "account", StringComparison.Ordinal) ? Guid.NewGuid() : _user.Id.Value,
            BrowserBinding = string.Equals(scenario, "browser", StringComparison.Ordinal) ? new string('x', 43) : _browser,
            State = string.Equals(scenario, "state", StringComparison.Ordinal) ? "other-state" : "state",
        }, CancellationToken.None);

        Assert.True(result.IsFailure);
        await _accounts.Received(string.Equals(scenario, "last-login", StringComparison.Ordinal) ? 1 : 0).UnlinkAsync(Arg.Any<UserId>(), Arg.Any<long>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
        Assert.Empty(_postCommit.ReceivedCalls());
        Assert.Empty(_operations.ReceivedCalls());
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("{}")]
    public async Task CorruptAttempt_DoesNotContactProvider(string json) {
        _stored[("telegram-unlink-oidc", $"{_browser}:{_user.Id.Value:D}")] = json;
        Assert.True((await Create().Send(Complete(), CancellationToken.None)).IsFailure);
        await _provider.DidNotReceive().ExchangeAsync(Arg.Any<TelegramOidcTokenExchange>(), Arg.Any<CancellationToken>());
        Assert.Empty(_accounts.ReceivedCalls());
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("account")]
    [InlineData("browser")]
    public async Task UnavailableStart_DoesNotCreateAttempt(string scenario) {
        if (string.Equals(scenario, "disabled", StringComparison.Ordinal)) { _provider.IsEnabled.Returns(returnThis: false); }
        Assert.True((await Create().Send(new StartTelegramUnlinkCommand(string.Equals(scenario, "account", StringComparison.Ordinal) ? Guid.Empty : _user.Id.Value,
            string.Equals(scenario, "browser", StringComparison.Ordinal) ? "" : _browser), CancellationToken.None)).IsFailure);
        Assert.Empty(_stored);
    }

    private UserAuthenticationPrincipalModel Principal() => UserAuthenticationIdentityService.ToAuthenticationPrincipal(_user, _now);
    private CompleteTelegramUnlinkCommand Complete() => new(_user.Id.Value, "code", "state", _browser);
    private ISender Create() => RequestTestSender.Create(
        new StartTelegramUnlinkCommandHandler(_provider, _tickets, _identities, new Clock(_now)),
        new CompleteTelegramUnlinkCommandHandler(_provider, _tickets, _identities, _accounts, new Clock(_now), _operations, _postCommit));

    [ExcludeFromCodeCoverage]
    private sealed class Clock(DateTime now) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
