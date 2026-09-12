using System.Net;
using System.Text;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Integrations.Authentication;
using FoodDiary.Integrations.Options;
using FoodDiary.Results;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Primitives;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Modules.Identity.Infrastructure.Tests.Integrations;

[ExcludeFromCodeCoverage]
public sealed class TelegramOidcProviderTests {
    private const string Verifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    [Fact]
    public void AuthorizationUrl_UsesPkceAndConfiguredCallback() {
        using var handler = new RecordingHandler();
        using var http = new HttpClient(handler);
        TelegramOidcProvider provider = CreateProvider(http, new RecordingValidator());
        Result<string> result = provider.CreateAuthorizationUrl("state", "nonce", Verifier);
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

        Result<TelegramOidcIdentity> result = await CreateProvider(http, validator).ExchangeAsync("code+special", Verifier, "nonce", CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("https://oauth.telegram.org/token", handler.Url);
        Assert.Equal("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("123456:test-secret")), handler.Authorization);
        Dictionary<string, StringValues> body = QueryHelpers.ParseQuery(handler.Body!);
        Assert.Equal("code+special", body["code"].ToString());
        Assert.Equal(Verifier, body["code_verifier"].ToString());
        Assert.Equal(1, validator.CallCount);
        Assert.Equal("signed-id", validator.Token);
        Assert.Equal("nonce", validator.Nonce);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("not-json")]
    [InlineData("{\"id_token\":123}")]
    public async Task InvalidProviderResponse_DoesNotAuthenticate(string responseBody) {
        using var handler = new RecordingHandler { ResponseBody = responseBody };
        using var http = new HttpClient(handler);
        var validator = new RecordingValidator();
        Result<TelegramOidcIdentity> result = await CreateProvider(http, validator).ExchangeAsync("code", Verifier, "nonce", CancellationToken.None);
        Assert.True(result.IsFailure);
        Assert.Equal(0, validator.CallCount);
    }

    private static TelegramOidcProvider CreateProvider(HttpClient http, ITelegramOidcTokenValidator validator) => new(http,
        MsOptions.Create(new TelegramOidcOptions {
            Enabled = true, ClientId = "123456", ClientSecret = "test-secret", RedirectUri = "https://app.example/auth/telegram/callback",
        }), validator);

    private sealed class RecordingValidator : ITelegramOidcTokenValidator {
        public TelegramOidcIdentity Identity { get; set; } = new("https://oauth.telegram.org", "subject", 123, FirstName: null, LastName: null, Username: null);
        public int CallCount { get; private set; }
        public string? Token { get; private set; }
        public string? Nonce { get; private set; }
        public Task<Result<TelegramOidcIdentity>> ValidateAsync(string idToken, string expectedNonce, CancellationToken cancellationToken) {
            CallCount++;
            Token = idToken;
            Nonce = expectedNonce;
            return Task.FromResult(Result.Success(Identity));
        }
    }

    private sealed class RecordingHandler : HttpMessageHandler {
        public string ResponseBody { get; init; } = "{\"id_token\":\"signed-id\"}";
        public string? Url { get; private set; }
        public string? Authorization { get; private set; }
        public string? Body { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
            Url = request.RequestUri?.AbsoluteUri;
            Authorization = request.Headers.Authorization?.ToString();
            Body = await request.Content!.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(ResponseBody, Encoding.UTF8, "application/json") };
        }
    }
}
