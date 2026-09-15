using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class TelegramRegistrationPostgresTests(PostgresApiWebApplicationFactory factory) : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task MiniAppRegistration_AndSubsequentLogin_PreserveEmailLessAccountAndRejectReplay() {
        var diagnostics = new ExceptionLog();
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["TelegramClient:LoginEnabled"] = "true",
                ["TelegramClient:RegistrationEnabled"] = "true",
                ["TelegramClient:OperationsEnabled"] = "true",
                ["TelegramClient:BotId"] = "123",
                ["TelegramBot:ApiSecret"] = "telegram-integration-test-secret",
                ["TelegramAuth:BotToken"] = "123:test-token",
            })).ConfigureLogging(logging => logging.AddProvider(diagnostics))
            .ConfigureServices(TelegramRecognitionProvider.Configure));
        using HttpClient client = app.CreateClient();
        using HttpResponseMessage begin = await client.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin", new { InitData = CreateInitData() });
        Assert.True(begin.StatusCode == HttpStatusCode.OK, begin.IsSuccessStatusCode ? null : await begin.Content.ReadAsStringAsync());
        using var intent = JsonDocument.Parse(await begin.Content.ReadAsStringAsync());
        string? ticket = intent.RootElement.GetProperty("ticket").GetString();
        Assert.Equal("onboarding", intent.RootElement.GetProperty("nextAction").GetString());
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/telegram/complete",
            new { Ticket = ticket, Action = "register", Language = "en", TimeZoneId = "UTC" });
        Assert.True(response.StatusCode == HttpStatusCode.OK, response.IsSuccessStatusCode ? null :
            await response.Content.ReadAsStringAsync() + Environment.NewLine + string.Join(Environment.NewLine, diagnostics.Errors));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.False(string.IsNullOrEmpty(body.RootElement.GetProperty("accessToken").GetString()));
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("user").GetProperty("email").ValueKind);
        await VerifySessionAsync(client, body.RootElement);
        using HttpClient otherUserClient = app.CreateClient();
        await TelegramMealJourney.VerifyAsync(client, otherUserClient, app.Services);
        await VerifyLinkConflictAsync(otherUserClient);
        using HttpResponseMessage replay = await client.PostAsJsonAsync("/api/v1/auth/telegram/complete",
            new { Ticket = ticket, Action = "register", Language = "en", TimeZoneId = "UTC" });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        await VerifySubsequentLoginAsync(app, body.RootElement.GetProperty("user").GetProperty("id").GetString());
        using HttpResponseMessage invalid = await client.PostAsJsonAsync("/api/v1/auth/telegram/complete",
            new { Ticket = "short", Action = "register", Language = "en", TimeZoneId = "UTC" });
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);
        using HttpResponseMessage invalidMiniApp = await client.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin", new { InitData = "" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidMiniApp.StatusCode);
        using HttpResponseMessage invalidOidc = await client.PostAsJsonAsync("/api/v1/auth/telegram/oidc/exchange", new { Code = "", State = "short" });
        Assert.Equal(HttpStatusCode.BadRequest, invalidOidc.StatusCode);
        await VerifyBackupEmailAsync(client, app, body.RootElement.GetProperty("user").GetProperty("id").GetGuid());
    }

    private async Task VerifyBackupEmailAsync(HttpClient client, WebApplicationFactory<Program> app, Guid userId) {
        string email = $"telegram-backup-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage request = await client.PostAsJsonAsync("/api/v1/auth/telegram/backup-email",
            new { Email = email, InitData = CreateInitData() });
        Assert.Equal(HttpStatusCode.NoContent, request.StatusCode);
        using HttpResponseMessage before = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        using var profile = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        Assert.Equal(JsonValueKind.Null, profile.RootElement.GetProperty("email").ValueKind);
        string token = factory.EmailSender.GetRequiredEmailVerificationToken(email);
        using HttpResponseMessage confirm = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { UserId = userId, Token = token });
        Assert.Equal(HttpStatusCode.NoContent, confirm.StatusCode);
        using HttpResponseMessage oldSession = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage replay = await client.PostAsJsonAsync("/api/v1/auth/verify-email", new { UserId = userId, Token = token });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        await VerifySubsequentLoginAsync(app, userId.ToString(), email);
    }

    private static string CreateInitData() {
        string authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        string queryId = Guid.NewGuid().ToString("N");
        string user = JsonSerializer.Serialize(new { id = 987654321, first_name = "Test", language_code = "en" });
        byte[] secret = HMACSHA256.HashData(Encoding.UTF8.GetBytes("WebAppData"), Encoding.UTF8.GetBytes("123:test-token"));
        string hash = Convert.ToHexStringLower(HMACSHA256.HashData(secret, Encoding.UTF8.GetBytes($"auth_date={authDate}\nquery_id={queryId}\nuser={user}")));
        return $"auth_date={authDate}&query_id={queryId}&user={Uri.EscapeDataString(user)}&hash={hash}";
    }

    private static async Task VerifyLinkConflictAsync(HttpClient client) {
        using HttpResponseMessage before = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);
        using var owner = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        using HttpResponseMessage begin = await client.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin-link",
            new { InitData = CreateInitData() });
        Assert.Equal(HttpStatusCode.OK, begin.StatusCode);
        using var intent = JsonDocument.Parse(await begin.Content.ReadAsStringAsync());
        Assert.Equal("link", intent.RootElement.GetProperty("nextAction").GetString());
        using HttpResponseMessage link = await client.PostAsJsonAsync("/api/v1/auth/telegram/complete-link",
            new { Ticket = intent.RootElement.GetProperty("ticket").GetString() });
        Assert.Equal(HttpStatusCode.Conflict, link.StatusCode);
        using HttpResponseMessage after = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, after.StatusCode);
        using var unchanged = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        Assert.Equal(owner.RootElement.GetProperty("id").GetGuid(), unchanged.RootElement.GetProperty("id").GetGuid());
        Assert.False(unchanged.RootElement.GetProperty("hasTelegramIdentity").GetBoolean());
    }

    private static async Task VerifySubsequentLoginAsync(WebApplicationFactory<Program> app, string? expectedUserId, string? expectedEmail = null) {
        using HttpClient client = app.CreateClient();
        string initData = CreateInitData();
        using HttpResponseMessage begin = await client.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin", new { InitData = initData });
        Assert.Equal(HttpStatusCode.OK, begin.StatusCode);
        using var intent = JsonDocument.Parse(await begin.Content.ReadAsStringAsync());
        Assert.Equal("login", intent.RootElement.GetProperty("nextAction").GetString());
        using HttpResponseMessage replay = await client.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin", new { InitData = initData });
        Assert.Equal(HttpStatusCode.Unauthorized, replay.StatusCode);
        using HttpResponseMessage login = await client.PostAsJsonAsync("/api/v1/auth/telegram/complete",
            new { Ticket = intent.RootElement.GetProperty("ticket").GetString(), Action = "login" });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var authentication = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.Equal(expectedUserId, authentication.RootElement.GetProperty("user").GetProperty("id").GetString());
        Assert.Equal(expectedEmail, authentication.RootElement.GetProperty("user").GetProperty("email").GetString());
        if (expectedEmail is not null) {
            Assert.True(authentication.RootElement.GetProperty("user").GetProperty("isEmailConfirmed").GetBoolean());
        }
        await VerifySessionAsync(client, authentication.RootElement, expectedEmail);
        if (expectedEmail is not null) {
            await VerifyPasswordAccessAsync(client, expectedUserId, expectedEmail);
        }
    }

    private static async Task VerifyPasswordAccessAsync(HttpClient client, string? expectedUserId, string email) {
        const string password = "TelegramBackup123!";
        using HttpResponseMessage withoutBackup = await client.PostAsJsonAsync("/api/v1/auth/telegram/unlink",
            new { InitData = CreateInitData() });
        Assert.Equal(HttpStatusCode.Conflict, withoutBackup.StatusCode);
        using HttpResponseMessage setPassword = await client.PatchAsJsonAsync("/api/v1/users/password/set",
            new { NewPassword = password });
        Assert.Equal(HttpStatusCode.NoContent, setPassword.StatusCode);
        using HttpResponseMessage oldSession = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, oldSession.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage login = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        using var authentication = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        Assert.Equal(expectedUserId, authentication.RootElement.GetProperty("user").GetProperty("id").GetString());
        Assert.True(authentication.RootElement.GetProperty("user").GetProperty("hasPassword").GetBoolean());
        await VerifySessionAsync(client, authentication.RootElement, email);
        using HttpResponseMessage unlink = await client.PostAsJsonAsync("/api/v1/auth/telegram/unlink",
            new { InitData = CreateInitData() });
        Assert.Equal(HttpStatusCode.NoContent, unlink.StatusCode);
        using HttpResponseMessage revoked = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, revoked.StatusCode);
        client.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage passwordLogin = await client.PostAsJsonAsync("/api/v1/auth/login",
            new { Email = email, Password = password });
        Assert.Equal(HttpStatusCode.OK, passwordLogin.StatusCode);
        using var restored = JsonDocument.Parse(await passwordLogin.Content.ReadAsStringAsync());
        Assert.Equal(expectedUserId, restored.RootElement.GetProperty("user").GetProperty("id").GetString());
        await VerifySessionAsync(client, restored.RootElement, email);
        using HttpResponseMessage telegramBegin = await client.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin",
            new { InitData = CreateInitData() });
        Assert.Equal(HttpStatusCode.OK, telegramBegin.StatusCode);
        using var intent = JsonDocument.Parse(await telegramBegin.Content.ReadAsStringAsync());
        Assert.Equal("onboarding", intent.RootElement.GetProperty("nextAction").GetString());
        using HttpResponseMessage relink = await client.PostAsJsonAsync("/api/v1/auth/telegram/complete-link",
            new { Ticket = intent.RootElement.GetProperty("ticket").GetString() });
        Assert.Equal(HttpStatusCode.OK, relink.StatusCode);
        using var linked = JsonDocument.Parse(await relink.Content.ReadAsStringAsync());
        Assert.Equal(expectedUserId, linked.RootElement.GetProperty("user").GetProperty("id").GetString());
        Assert.True(linked.RootElement.GetProperty("user").GetProperty("hasTelegramIdentity").GetBoolean());
        await VerifySessionAsync(client, linked.RootElement, email);
        await TelegramDeletionJourney.VerifyAsync(client);
        using HttpResponseMessage deletedLogin = await client.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin",
            new { InitData = CreateInitData() });
        Assert.Equal(HttpStatusCode.Unauthorized, deletedLogin.StatusCode);
    }

    private static async Task VerifySessionAsync(HttpClient client, JsonElement authentication, string? expectedEmail = null) {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authentication.GetProperty("accessToken").GetString());
        using HttpResponseMessage profile = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        using var profileBody = JsonDocument.Parse(await profile.Content.ReadAsStringAsync());
        Assert.Equal(expectedEmail, profileBody.RootElement.GetProperty("email").GetString());
        using HttpResponseMessage refresh = await client.PostAsJsonAsync("/api/v1/auth/refresh",
            new { RefreshToken = authentication.GetProperty("refreshToken").GetString() });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        using var refreshed = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.RootElement.GetProperty("accessToken").GetString());
        using HttpResponseMessage refreshedProfile = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, refreshedProfile.StatusCode);
    }

    [ExcludeFromCodeCoverage]
    private sealed class ExceptionLog : ILoggerProvider, ILogger {
        internal ConcurrentQueue<string> Errors { get; } = new();
        public ILogger CreateLogger(string categoryName) => this;
        public void Dispose() { }
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Error;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) {
            if (exception is not null) {
                Errors.Enqueue(exception.ToString());
            }
        }
    }
}
