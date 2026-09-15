using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Identity.Authentication.Commands.BeginTelegramMiniApp;
using FoodDiary.Application.Identity.Authentication.Commands.ExchangeTelegramOidc;
using FoodDiary.Application.Identity.Authentication.Commands.StartTelegramOidc;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Queries.GetTelegramConfiguration;
using FoodDiary.Application.Identity.Authentication.Services;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Authentication;

[ExcludeFromCodeCoverage]
public sealed class TelegramAuthenticationBoundaryTests {
    private readonly ITelegramOidcProvider _provider = Substitute.For<ITelegramOidcProvider>();
    private readonly ITelegramLoginTicketStore _tickets = Substitute.For<ITelegramLoginTicketStore>();
    private readonly ITelegramIdentityPolicy _policy = Substitute.For<ITelegramIdentityPolicy>();
    private readonly IUserTelegramAccountService _accounts = Substitute.For<IUserTelegramAccountService>();

    public TelegramAuthenticationBoundaryTests() {
        _policy.LoginEnabled.Returns(returnThis: true);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task Configuration_NeverAdvertisesRegistrationOrOidcWhenLoginIsDisabled(bool login, bool registration, bool oidc) {
        _policy.LoginEnabled.Returns(login);
        _policy.RegistrationEnabled.Returns(registration);
        _provider.IsEnabled.Returns(oidc);
        var handler = new GetTelegramConfigurationQueryHandler(_policy, _provider);
        Result<TelegramConfigurationModel> result = await handler.Handle(new GetTelegramConfigurationQuery(), CancellationToken.None);
        Assert.Equal(new TelegramConfigurationModel(login, login && registration, login && oidc), result.Value);
    }

    [Theory]
    [InlineData(false, 43)]
    [InlineData(true, 0)]
    public async Task Start_InvalidConfigurationOrBindingDoesNotCreateAttempt(bool enabled, int bindingLength) {
        _policy.LoginEnabled.Returns(enabled);
        var handler = new StartTelegramOidcCommandHandler(_provider, _tickets, _policy, TimeProvider.System);
        Result<TelegramOidcStartModel> result = await handler.Handle(new StartTelegramOidcCommand(new string('b', bindingLength)), CancellationToken.None);
        Assert.Equal(TelegramIdentityErrors.NotConfigured.Code, result.Error.Code);
        Assert.Empty(_tickets.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Start_BindsAttemptAndPreservesProviderResult(bool succeeds) {
        string binding = new('b', 43);
        _tickets.CreateAsync("telegram-oidc-attempt", binding, Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns("state");
        _provider.CreateAuthorizationUrl("state", Arg.Any<string>(), Arg.Any<string>()).Returns(succeeds
            ? Result.Success("https://oauth.telegram.org/auth") : Result.Failure<string>(TelegramIdentityErrors.NotConfigured));
        var handler = new StartTelegramOidcCommandHandler(_provider, _tickets, _policy, TimeProvider.System);
        Result<TelegramOidcStartModel> result = await handler.Handle(new StartTelegramOidcCommand(binding), CancellationToken.None);
        Assert.Equal(succeeds, result.IsSuccess);
        await _tickets.Received(1).CreateAsync("telegram-oidc-attempt", binding, Arg.Any<string>(), Arg.Any<DateTime>(), CancellationToken.None);
        if (succeeds) {
            Assert.Equal("https://oauth.telegram.org/auth", result.Value.AuthorizationUrl);
        } else {
            Assert.Equal(TelegramIdentityErrors.NotConfigured.Code, result.Error.Code);
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("{")]
    [InlineData("null")]
    public async Task Exchange_InvalidOrConsumedStateDoesNotContactProvider(string? payload) {
        _tickets.ConsumeAsync("state", "telegram-oidc-attempt", "browser", Arg.Any<CancellationToken>()).Returns(payload);
        Result<TelegramAuthenticationIntentModel> result = await Exchange().Handle(new ExchangeTelegramOidcCommand("code", "state", "browser"), CancellationToken.None);
        Assert.Equal(TelegramIdentityErrors.InvalidProof.Code, result.Error.Code);
        Assert.Empty(_provider.ReceivedCalls());
    }

    [Fact]
    public async Task Exchange_DisabledLoginDoesNotConsumeState() {
        _policy.LoginEnabled.Returns(returnThis: false);
        Result<TelegramAuthenticationIntentModel> result = await Exchange().Handle(new ExchangeTelegramOidcCommand("code", "state", "browser"), CancellationToken.None);
        Assert.Equal(TelegramIdentityErrors.NotConfigured.Code, result.Error.Code);
        Assert.Empty(_tickets.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Exchange_UsesStoredNonceAndVerifierAndPropagatesProviderOutcome(bool succeeds) {
        string payload = JsonSerializer.Serialize(new { Nonce = "nonce", CodeVerifier = "verifier", LinkUserId = (Guid?)null });
        _tickets.ConsumeAsync("state", "telegram-oidc-attempt", "browser", Arg.Any<CancellationToken>()).Returns(payload);
        _provider.ExchangeAsync("code", "verifier", "nonce", Arg.Any<CancellationToken>()).Returns(succeeds
            ? Result.Success(new TelegramOidcIdentity("issuer", "subject", 123, "Alex", LastName: null, Username: null))
            : Result.Failure<TelegramOidcIdentity>(TelegramIdentityErrors.InvalidProof));
        _accounts.IsRegisteredAsync(123, Arg.Any<CancellationToken>()).Returns(Result.Success(value: false));
        _tickets.CreateAsync(Arg.Any<string>(), "browser", Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns("ticket");
        Result<TelegramAuthenticationIntentModel> result = await Exchange().Handle(new ExchangeTelegramOidcCommand("code", "state", "browser"), CancellationToken.None);
        Assert.Equal(succeeds, result.IsSuccess);
        if (succeeds) {
            Assert.Equal("onboarding", result.Value.NextAction);
            Assert.Equal("ticket", result.Value.Ticket);
        } else {
            Assert.Equal(TelegramIdentityErrors.InvalidProof.Code, result.Error.Code);
            Assert.Empty(_accounts.ReceivedCalls());
        }
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("invalid")]
    [InlineData("replayed")]
    public async Task MiniApp_RejectsDisabledInvalidAndReplayedProofWithoutIssuingTicket(string scenario) {
        ITelegramAuthValidator validator = Substitute.For<ITelegramAuthValidator>();
        ITelegramAssertionReplayGuard replay = Substitute.For<ITelegramAssertionReplayGuard>();
        _policy.LoginEnabled.Returns(!string.Equals(scenario, "disabled", StringComparison.Ordinal));
        validator.ValidateInitData("proof").Returns(string.Equals(scenario, "invalid", StringComparison.Ordinal)
            ? Result.Failure<TelegramInitData>(TelegramIdentityErrors.InvalidProof)
            : Result.Success(new TelegramInitData(123, Username: null, FirstName: null, LastName: null, PhotoUrl: null, LanguageCode: null, DateTime.UtcNow)));
        replay.TryConsumeAsync(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(returnThis: false);
        var handler = new BeginTelegramMiniAppCommandHandler(validator, replay, _policy, Intents());
        Result<TelegramAuthenticationIntentModel> result = await handler.Handle(new BeginTelegramMiniAppCommand("proof", "browser"), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Empty(_tickets.ReceivedCalls());
    }

    private TelegramAuthenticationIntentService Intents() => new(_tickets, _accounts, TimeProvider.System);
    private ExchangeTelegramOidcCommandHandler Exchange() => new(_provider, _tickets, _policy, Intents());
}
