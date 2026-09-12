using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Results;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class TelegramOidcPostgresTests(PostgresApiWebApplicationFactory factory) : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task OidcRegistrationAndLogin_ConsumeBrowserBoundStateAndPreserveUser() {
        ITelegramOidcProvider provider = Substitute.For<ITelegramOidcProvider>();
        provider.IsEnabled.Returns(returnThis: true);
        string? expectedNonce = null;
        string? expectedVerifier = null;
        provider.CreateAuthorizationUrl(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(call => {
            expectedNonce = call.ArgAt<string>(1);
            expectedVerifier = call.ArgAt<string>(2);
            Assert.Equal(43, expectedNonce.Length);
            Assert.Equal(43, expectedVerifier.Length);
            return Result.Success("https://oauth.telegram.org/auth?state=" + call.ArgAt<string>(0));
        });
        provider.ExchangeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(call => {
            Assert.Equal("test-code", call.ArgAt<string>(0));
            Assert.Equal(expectedVerifier, call.ArgAt<string>(1));
            Assert.Equal(expectedNonce, call.ArgAt<string>(2));
            return Result.Success(new TelegramOidcIdentity("https://oauth.telegram.org", "distinct-subject", 11223344,
                FirstName: "Test", LastName: null, Username: null));
        });
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder => builder
            .ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["TelegramClient:LoginEnabled"] = "true",
                ["TelegramClient:RegistrationEnabled"] = "true",
                ["TelegramAuth:BotToken"] = "123:test-token",
                ["TelegramOidc:Enabled"] = "true",
                ["TelegramOidc:ClientId"] = "123",
                ["TelegramOidc:ClientSecret"] = "test-secret",
                ["TelegramOidc:RedirectUri"] = "https://app.example/auth/telegram/callback",
            }))
            .ConfigureServices(services => services.Replace(ServiceDescriptor.Singleton(provider))));
        using HttpClient client = app.CreateClient();
        using HttpClient foreignBrowser = app.CreateClient();
        using HttpResponseMessage foreignStart = await foreignBrowser.PostAsync("/api/v1/auth/telegram/oidc/start", content: null);
        Assert.Equal(HttpStatusCode.OK, foreignStart.StatusCode);
        Guid registered = await CompleteAsync(client, foreignBrowser, "register");
        Guid loggedIn = await CompleteAsync(client, foreignBrowser, "login");
        Assert.Equal(registered, loggedIn);
        await provider.Received(2).ExchangeAsync("test-code", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await VerifyBrowserBackupEmailAsync(client, foreignBrowser, registered);
    }

    private async Task VerifyBrowserBackupEmailAsync(HttpClient client, HttpClient foreignBrowser, Guid userId) {
        string email = $"oidc-backup-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage start = await client.PostAsJsonAsync("/api/v1/auth/telegram/backup-email/oidc/start", new { Email = email });
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var authorization = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        string state = new Uri(authorization.RootElement.GetProperty("authorizationUrl").GetString()!).Query["?state=".Length..];
        var request = new { Code = "test-code", State = state };
        using HttpResponseMessage anonymous = await foreignBrowser.PostAsJsonAsync("/api/v1/auth/telegram/backup-email/oidc/complete", request);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        foreignBrowser.DefaultRequestHeaders.Authorization = client.DefaultRequestHeaders.Authorization;
        using HttpResponseMessage foreign = await foreignBrowser.PostAsJsonAsync("/api/v1/auth/telegram/backup-email/oidc/complete", request);
        Assert.Equal(HttpStatusCode.Unauthorized, foreign.StatusCode);
        using HttpResponseMessage complete = await client.PostAsJsonAsync("/api/v1/auth/telegram/backup-email/oidc/complete", request);
        Assert.Equal(HttpStatusCode.NoContent, complete.StatusCode);
        using HttpResponseMessage replay = await client.PostAsJsonAsync("/api/v1/auth/telegram/backup-email/oidc/complete", request);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using HttpResponseMessage before = await client.GetAsync("/api/v1/users/info");
        using var profile = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, profile.RootElement.GetProperty("email").ValueKind);
        string token = factory.EmailSender.GetRequiredEmailVerificationToken(email);
        using HttpResponseMessage confirm = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { UserId = userId, Token = token });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        using HttpResponseMessage oldSession = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
    }

    private static async Task<Guid> CompleteAsync(HttpClient client, HttpClient foreignBrowser, string action) {
        using HttpResponseMessage start = await client.PostAsync("/api/v1/auth/telegram/oidc/start", content: null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        using var authorization = JsonDocument.Parse(await start.Content.ReadAsStringAsync());
        string url = authorization.RootElement.GetProperty("authorizationUrl").GetString()!;
        string state = new Uri(url).Query["?state=".Length..];
        var exchangeRequest = new { Code = "test-code", State = state };
        using HttpResponseMessage foreign = await foreignBrowser.PostAsJsonAsync("/api/v1/auth/telegram/oidc/exchange", exchangeRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, foreign.StatusCode);
        using HttpResponseMessage exchange = await client.PostAsJsonAsync("/api/v1/auth/telegram/oidc/exchange", exchangeRequest);
        Assert.Equal(HttpStatusCode.OK, exchange.StatusCode);
        using var intent = JsonDocument.Parse(await exchange.Content.ReadAsStringAsync());
        Assert.Equal(string.Equals(action, "register", StringComparison.Ordinal) ? "onboarding" : "login",
            intent.RootElement.GetProperty("nextAction").GetString());
        using HttpResponseMessage replay = await client.PostAsJsonAsync("/api/v1/auth/telegram/oidc/exchange", exchangeRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        var completionRequest = new {
            Ticket = intent.RootElement.GetProperty("ticket").GetString(),
            Action = action,
            Language = "en",
            TimeZoneId = "UTC",
        };
        using HttpResponseMessage complete = await client.PostAsJsonAsync("/api/v1/auth/telegram/complete", completionRequest);
        Assert.Equal(HttpStatusCode.OK, complete.StatusCode);
        using var authentication = JsonDocument.Parse(await complete.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, authentication.RootElement.GetProperty("user").GetProperty("email").ValueKind);
        Assert.False(string.IsNullOrWhiteSpace(authentication.RootElement.GetProperty("accessToken").GetString()));
        using HttpResponseMessage repeatedCompletion = await client.PostAsJsonAsync("/api/v1/auth/telegram/complete", completionRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, repeatedCompletion.StatusCode);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication.RootElement.GetProperty("accessToken").GetString());
        return authentication.RootElement.GetProperty("user").GetProperty("id").GetGuid();
    }
}
