using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Authentication;
using FoodDiary.Modules.Identity.Infrastructure.Providers.Options;
using System.Net;
using System.Text;
using FoodDiary.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class TelegramOidcProviderTests {
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    [Fact]
    public async Task DisabledProvider_DoesNotSendCredentials() {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var logger = new TelegramOidcTestLogger<TelegramOidcProvider>();
        var provider = new TelegramOidcProvider(http, MsOptions.Create(new TelegramOidcOptions()), new RecordingValidator(), logger);
        Assert.False(provider.IsEnabled);
        Assert.True(provider.CreateAuthorizationUrl(new TelegramOidcAuthorizationRequest(new TelegramOAuthState("state"), new TelegramOidcNonce("nonce"), new TelegramPkceVerifier(Verifier))).IsFailure);
        Assert.True((await provider.ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode("code"), new TelegramPkceVerifier(Verifier), new TelegramOidcNonce("nonce")), CancellationToken.None)).IsFailure);
        Assert.Null(handler.Url);
        logger.AssertRejection("token_exchange", "not_configured");
    }

    [Theory]
    [InlineData("", 43, "nonce")]
    [InlineData("code", 42, "nonce")]
    [InlineData("code", 43, "")]
    public async Task InvalidExchangeInput_NeverCallsProvider(string code, int verifierLength, string nonce) {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var logger = new TelegramOidcTestLogger<TelegramOidcProvider>();
        Assert.True((await CreateProvider(http, new RecordingValidator(), logger).ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode(code), new TelegramPkceVerifier(new string('a', verifierLength)), new TelegramOidcNonce(nonce)), CancellationToken.None)).IsFailure);
        Assert.Null(handler.Url);
        logger.AssertRejection("token_exchange", "invalid_input");
    }

    [Theory]
    [InlineData("http", "token_exchange", "http_request_failed")]
    [InlineData("timeout", "token_exchange", "timeout")]
    [InlineData("status", "token_exchange", "http_error")]
    [InlineData("oversized", "token_response", "http_request_failed")]
    public async Task ProviderFailure_DoesNotValidateToken(string scenario, string stage, string reason) {
        using var handler = new RecordingHandler {
            Failure = scenario switch { "http" => new HttpRequestException("private-transport-detail"), "timeout" => new OperationCanceledException("private-timeout-detail"), _ => null },
            Status = string.Equals(scenario, "status", StringComparison.Ordinal) ? HttpStatusCode.BadGateway : HttpStatusCode.OK,
            ResponseBody = string.Equals(scenario, "oversized", StringComparison.Ordinal) ? new string('x', 32769) : "private-provider-response",
        };
        using var http = new HttpClient(handler);
        var validator = new RecordingValidator();
        var logger = new TelegramOidcTestLogger<TelegramOidcProvider>();
        Assert.True((await CreateProvider(http, validator, logger).ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode("private-code"), new TelegramPkceVerifier(Verifier), new TelegramOidcNonce("private-nonce")), CancellationToken.None)).IsFailure);
        Assert.Equal(0, validator.CallCount);
        logger.AssertRejection(stage, reason, "private-transport-detail", "private-timeout-detail", "private-provider-response", "private-code", "private-nonce", Verifier, "test-secret");
        Assert.Equal(string.Equals(scenario, "status", StringComparison.Ordinal) ? (int?)502 : null, logger.Entries[0].Fields["HttpStatusCode"]);
    }

    [Fact]
    public async Task CallerCancellation_IsPropagated() {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        using var handler = new RecordingHandler { Failure = new OperationCanceledException(cancellation.Token) };
        using var http = new HttpClient(handler);
        var logger = new TelegramOidcTestLogger<TelegramOidcProvider>();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => CreateProvider(http, new RecordingValidator(), logger)
            .ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode("code"), new TelegramPkceVerifier(Verifier), new TelegramOidcNonce("nonce")), cancellation.Token));
        Assert.Empty(logger.Entries);
    }

    [Fact]
    public async Task CancellationDuringTokenValidation_IsPropagated() {
        using var cancellation = new CancellationTokenSource();
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var validation = new TaskCompletionSource<Result<TelegramOidcIdentity>>(TaskCreationOptions.RunContinuationsAsynchronously);
        var validator = new RecordingValidator {
            OnValidate = token => {
                Assert.Equal(cancellation.Token, token);
                entered.SetResult();
                return validation.Task;
            },
        };
        Task<Result<TelegramOidcIdentity>> exchange = CreateProvider(http, validator)
            .ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode("code"), new TelegramPkceVerifier(Verifier), new TelegramOidcNonce("nonce")), cancellation.Token);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        Assert.False(exchange.IsCompleted);
        await cancellation.CancelAsync();
        validation.SetCanceled(cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => exchange);
        Assert.Equal(1, validator.CallCount);
    }

    [Fact]
    public async Task UnexpectedValidatorFailure_IsNotMisreportedAsInvalidCredentials() {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var failure = new InvalidDataException("Validator implementation failed.");
        var validator = new RecordingValidator {
            OnValidate = _ => Task.FromException<Result<TelegramOidcIdentity>>(failure),
        };
        InvalidDataException actual = await Assert.ThrowsAsync<InvalidDataException>(() => CreateProvider(http, validator)
            .ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode("code"), new TelegramPkceVerifier(Verifier), new TelegramOidcNonce("nonce")), CancellationToken.None));
        Assert.Same(failure, actual);
    }

    [Fact]
    public void AuthorizationUrl_UsesPkceAndConfiguredCallback() {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        TelegramOidcProvider provider = CreateProvider(http, new RecordingValidator());
        Result<string> result = provider.CreateAuthorizationUrl(new TelegramOidcAuthorizationRequest(new TelegramOAuthState("state"), new TelegramOidcNonce("nonce"), new TelegramPkceVerifier(Verifier)));
        var uri = new Uri(result.Value);
        Dictionary<string, StringValues> query = QueryHelpers.ParseQuery(uri.Query);
        Assert.Equal("oauth.telegram.org", uri.Host);
        Assert.Equal("openid profile", query["scope"].ToString());
        Assert.Equal("https://app.example/auth/telegram/callback", query["redirect_uri"].ToString());
        Assert.Equal("S256", query["code_challenge_method"].ToString());
        Assert.Equal("E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM", query["code_challenge"].ToString());
        Assert.DoesNotContain("test-secret", result.Value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Exchange_SendsServerCredentialsAndValidatesReturnedIdToken() {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        var validator = new RecordingValidator();
        validator.Identity = new TelegramOidcIdentity("https://oauth.telegram.org", "different-subject", 123, FirstName: null, LastName: null, Username: null);

        var logger = new TelegramOidcTestLogger<TelegramOidcProvider>();
        Result<TelegramOidcIdentity> result = await CreateProvider(http, validator, logger).ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode("code+special"), new TelegramPkceVerifier(Verifier), new TelegramOidcNonce("nonce")), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://oauth.telegram.org/token", handler.Url);
        Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("123456:test-secret")), handler.Authorization);
        Dictionary<string, StringValues> body = QueryHelpers.ParseQuery(handler.Body!);
        Assert.Equal("code+special", body["code"].ToString());
        Assert.Equal(Verifier, body["code_verifier"].ToString());
        Assert.Equal(1, validator.CallCount);
        Assert.Equal("signed-id", validator.Token);
        Assert.Equal("nonce", validator.Nonce);
        Assert.Empty(logger.Entries);
    }

    [Theory]
    [InlineData("{}", "id_token_missing_or_invalid")]
    [InlineData("not-json", "invalid_json")]
    [InlineData("{\"id_token\":123}", "id_token_missing_or_invalid")]
    [InlineData("[]", "invalid_response")]
    public async Task InvalidProviderResponse_DoesNotAuthenticate(string responseBody, string reason) {
        using var handler = new RecordingHandler { ResponseBody = responseBody };
        using var http = new HttpClient(handler);
        var validator = new RecordingValidator();
        var logger = new TelegramOidcTestLogger<TelegramOidcProvider>();
        Result<TelegramOidcIdentity> result = await CreateProvider(http, validator, logger).ExchangeAsync(new TelegramOidcTokenExchange(new TelegramAuthorizationCode("code"), new TelegramPkceVerifier(Verifier), new TelegramOidcNonce("nonce")), CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(0, validator.CallCount);
        logger.AssertRejection("token_response", reason, "test-secret", Verifier);
    }

    private static TelegramOidcProvider CreateProvider(HttpClient http, ITelegramOidcTokenValidator validator, ILogger<TelegramOidcProvider>? logger = null) => new(http,
        MsOptions.Create(new TelegramOidcOptions {
            Enabled = true,
            ClientId = "123456",
            ClientSecret = "test-secret",
            RedirectUri = "https://app.example/auth/telegram/callback",
        }), validator, logger ?? NullLogger<TelegramOidcProvider>.Instance);

    [ExcludeFromCodeCoverage]
    private sealed class RecordingValidator : ITelegramOidcTokenValidator {
        public Func<CancellationToken, Task<Result<TelegramOidcIdentity>>>? OnValidate { get; init; }
        public TelegramOidcIdentity Identity { get; set; } = new("https://oauth.telegram.org", "subject", 123, FirstName: null, LastName: null, Username: null);
        public int CallCount { get; private set; }
        public string? Token { get; private set; }
        public string? Nonce { get; private set; }
        public Task<Result<TelegramOidcIdentity>> ValidateAsync(string idToken, TelegramOidcNonce expectedNonce, CancellationToken cancellationToken) {
            CallCount++;
            Token = idToken;
            Nonce = expectedNonce.Value;
            return OnValidate?.Invoke(cancellationToken) ?? Task.FromResult(Result.Success(Identity));
        }
    }

    [ExcludeFromCodeCoverage]
    private sealed class RecordingHandler : HttpMessageHandler {
        public Exception? Failure { get; init; }
        public HttpStatusCode Status { get; init; } = HttpStatusCode.OK;
        public string ResponseBody { get; init; } = "{\"id_token\":\"signed-id\"}";
        public string? Url { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            if (Failure is not null) {
                throw Failure;
            }
            Url = request.RequestUri?.AbsoluteUri;
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(Status) { Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json") };
        }
    }
}
