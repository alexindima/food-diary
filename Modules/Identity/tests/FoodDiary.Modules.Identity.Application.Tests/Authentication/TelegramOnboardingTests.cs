using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Application.Abstractions.Authentication.Models;
using FoodDiary.Application.Abstractions.Authentication.Services;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Application.Identity.Authentication.Commands.BeginTelegramMiniApp;
using FoodDiary.Application.Identity.Authentication.Commands.CompleteTelegramAuthentication;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Application.Users.Services;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramOnboardingTests {
    private readonly IUserTelegramAccountService _accounts = Substitute.For<IUserTelegramAccountService>();
    private readonly IUserAuthenticationIdentityService _identities = Substitute.For<IUserAuthenticationIdentityService>();
    private readonly IAuthenticationTokenService _tokens = Substitute.For<IAuthenticationTokenService>();
    private readonly ITelegramIdentityPolicy _policy = Substitute.For<ITelegramIdentityPolicy>();
    private readonly MemoryTickets _tickets = new();

    public TelegramOnboardingTests() {
        _policy.LoginEnabled.Returns(returnThis: true);
        _policy.RegistrationEnabled.Returns(returnThis: true);
        _tokens.IssueFromPrincipalAsync(Arg.Any<UserAuthenticationPrincipalModel>(), Arg.Any<CancellationToken>(),
            Arg.Any<AuthenticationClientContext?>(), Arg.Any<bool>()).Returns(new IssuedAuthenticationTokens("access", "refresh"));
    }

    [Fact]
    public async Task UnknownIdentity_GetsOnboardingThenRegistersOnce() {
        _accounts.IsRegisteredAsync(123, Arg.Any<CancellationToken>()).Returns(Result.Success(value: false));
        var user = User.CreateTelegram(123, "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, DateTime.UtcNow);
        _accounts.RegisterAsync(Arg.Any<UserTelegramRegistrationModel>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        TelegramAuthenticationIntentModel intent = await CreateIntentAsync();
        Assert.Equal("onboarding", intent.NextAction);

        CompleteTelegramAuthenticationCommandHandler handler = CreateHandler();
        var command = new CompleteTelegramAuthenticationCommand(intent.Ticket, "browser", "register", Language: "ru", TimeZoneId: "Asia/Tbilisi");
        Result<AuthenticationModel> result = await handler.Handle(command, CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.User.Email);
        Assert.True((await handler.Handle(command, CancellationToken.None)).IsFailure);
        await _accounts.Received(1).RegisterAsync(Arg.Is<UserTelegramRegistrationModel>(model => model.TelegramUserId == 123 && model.TimeZoneId == "Asia/Tbilisi"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WrongBrowser_CannotConsumeThenCorrectBrowserCanRegister() {
        _accounts.IsRegisteredAsync(123, Arg.Any<CancellationToken>()).Returns(Result.Success(value: false));
        _accounts.RegisterAsync(Arg.Any<UserTelegramRegistrationModel>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserAuthenticationPrincipalModel>(UserErrors.TelegramAlreadyLinked));
        TelegramAuthenticationIntentModel intent = await CreateIntentAsync();
        Result<AuthenticationModel> wrong = await CreateHandler().Handle(
            new CompleteTelegramAuthenticationCommand(intent.Ticket, "other-browser", "register", TimeZoneId: "UTC"), CancellationToken.None);
        Assert.Equal("Authentication.TelegramInvalidProof", wrong.Error.Code);
        Result<AuthenticationModel> correct = await CreateHandler().Handle(
            new CompleteTelegramAuthenticationCommand(intent.Ticket, "browser", "register", TimeZoneId: "UTC"), CancellationToken.None);
        Assert.Equal("User.TelegramAlreadyLinked", correct.Error.Code);
    }

    [Fact]
    public async Task LinkTicket_CannotTargetAnotherAuthenticatedAccount() {
        _accounts.IsRegisteredAsync(123, Arg.Any<CancellationToken>()).Returns(Result.Success(value: false));
        TelegramAuthenticationIntentModel intent = await CreateIntentAsync(Guid.NewGuid());
        Result<AuthenticationModel> result = await CreateHandler().Handle(
            new CompleteTelegramAuthenticationCommand(intent.Ticket, "browser", "link", Guid.NewGuid()), CancellationToken.None);
        Assert.True(result.IsFailure);
        await _identities.DidNotReceive().LinkTelegramAsync(Arg.Any<UserId>(), Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RegistrationDisabled_DoesNotConsumeTicketOrCreateAccount() {
        _accounts.IsRegisteredAsync(123, Arg.Any<CancellationToken>()).Returns(Result.Success(value: false));
        TelegramAuthenticationIntentModel intent = await CreateIntentAsync();
        _policy.RegistrationEnabled.Returns(returnThis: false);
        Result<AuthenticationModel> result = await CreateHandler().Handle(
            new CompleteTelegramAuthenticationCommand(intent.Ticket, "browser", "register", TimeZoneId: "UTC"), CancellationToken.None);
        Assert.Equal("Authentication.TelegramOidcNotConfigured", result.Error.Code);
        await _accounts.DidNotReceive().RegisterAsync(Arg.Any<UserTelegramRegistrationModel>(), Arg.Any<CancellationToken>());
        Assert.Equal(0, _tickets.ConsumptionCount);
    }

    [Fact]
    public async Task MiniAppProof_IsConsumedOnlyDuringBegin() {
        _accounts.IsRegisteredAsync(123, Arg.Any<CancellationToken>()).Returns(Result.Success(value: false));
        ITelegramAuthValidator validator = Substitute.For<ITelegramAuthValidator>();
        ITelegramAssertionReplayGuard replay = Substitute.For<ITelegramAssertionReplayGuard>();
        validator.ValidateInitData("signed").Returns(Result.Success(new TelegramInitData(123, Username: null, "Alex", LastName: null, PhotoUrl: null, "ru", DateTime.UtcNow)));
        replay.TryConsumeAsync("signed", Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(returnThis: true);
        var handler = new BeginTelegramMiniAppCommandHandler(validator, replay, _policy, CreateIntents());
        Result<TelegramAuthenticationIntentModel> result = await handler.Handle(new BeginTelegramMiniAppCommand("signed", "browser"), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal("onboarding", result.Value.NextAction);
        await replay.Received(1).TryConsumeAsync("signed", Arg.Any<DateTime>(), Arg.Any<CancellationToken>());
        await _accounts.DidNotReceive().RegisterAsync(Arg.Any<UserTelegramRegistrationModel>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OidcIntent_PreservesIssuerAndSubjectSeparatelyFromBotUserId() {
        _accounts.IsRegisteredAsync(123, Arg.Any<CancellationToken>()).Returns(Result.Success(value: false));
        Result<TelegramAuthenticationIntentModel> result = await CreateIntents().CreateAsync(
            123, "Alex", lastName: null, "ru", "browser", linkUserId: null, CancellationToken.None,
            "https://oauth.telegram.org", "different-oidc-subject");

        Assert.True(result.IsSuccess);
        string? payload = await _tickets.ConsumeAsync(result.Value.Ticket, "telegram-onboarding", "browser", CancellationToken.None);
        Assert.NotNull(payload);
        using var document = JsonDocument.Parse(payload);
        Assert.Equal(123, document.RootElement.GetProperty("TelegramUserId").GetInt64());
        Assert.Equal("https://oauth.telegram.org", document.RootElement.GetProperty("OidcIssuer").GetString());
        Assert.Equal("different-oidc-subject", document.RootElement.GetProperty("OidcSubject").GetString());
    }

    [Fact]
    public async Task OidcLogin_MismatchedStoredIdentityDoesNotIssueTokens() {
        _accounts.IsRegisteredAsync(123, Arg.Any<CancellationToken>()).Returns(Result.Success(value: true));
        var user = User.CreateTelegram(123, "hash");
        UserAuthenticationPrincipalModel principal = UserAuthenticationIdentityService.ToAuthenticationPrincipal(user, DateTime.UtcNow);
        _identities.AuthenticateTelegramAsync(123, Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(Result.Success(principal));
        _accounts.BindOidcIdentityAsync(user.Id, 123, "https://oauth.telegram.org", "subject", Arg.Any<CancellationToken>())
            .Returns(Result.Failure(UserErrors.TelegramAlreadyLinked));
        Result<TelegramAuthenticationIntentModel> intent = await CreateIntents().CreateAsync(
            123, "Alex", lastName: null, "ru", "browser", linkUserId: null, CancellationToken.None,
            "https://oauth.telegram.org", "subject");

        Result<AuthenticationModel> result = await CreateHandler().Handle(
            new CompleteTelegramAuthenticationCommand(intent.Value.Ticket, "browser", "login"), CancellationToken.None);

        Assert.True(result.IsFailure);
        await _tokens.DidNotReceive().IssueFromPrincipalAsync(Arg.Any<UserAuthenticationPrincipalModel>(), Arg.Any<CancellationToken>(),
            Arg.Any<AuthenticationClientContext?>(), Arg.Any<bool>());
    }

    private TelegramAuthenticationIntentService CreateIntents() => new(_tickets, _accounts, TimeProvider.System);
    private async Task<TelegramAuthenticationIntentModel> CreateIntentAsync(Guid? linkUserId = null) =>
        (await CreateIntents().CreateAsync(123, "Alex", lastName: null, "ru", "browser", linkUserId, CancellationToken.None)).Value;
    private CompleteTelegramAuthenticationCommandHandler CreateHandler() => new(_tickets, _policy, _accounts, _identities, _tokens, TimeProvider.System);

    private sealed class MemoryTickets : ITelegramLoginTicketStore {
        private readonly Dictionary<string, (string Purpose, string Binding, string Payload)> _values = new(StringComparer.Ordinal);
        public int ConsumptionCount { get; private set; }
        public Task<string> CreateAsync(string purpose, string browserBinding, string payload, DateTime expiresAtUtc, CancellationToken cancellationToken) {
            string ticket = SecurityTokenGenerator.GenerateUrlSafeToken();
            _values.Add(ticket, (purpose, browserBinding, payload));
            return Task.FromResult(ticket);
        }
        public Task<string?> ConsumeAsync(string ticket, string purpose, string browserBinding, CancellationToken cancellationToken) {
            if (!_values.TryGetValue(ticket, out (string Purpose, string Binding, string Payload) value) || !string.Equals(value.Purpose, purpose, StringComparison.Ordinal) ||
                !string.Equals(value.Binding, browserBinding, StringComparison.Ordinal)) {
                return Task.FromResult<string?>(null);
            }
            _values.Remove(ticket);
            ConsumptionCount++;
            return Task.FromResult<string?>(value.Payload);
        }
    }
}
