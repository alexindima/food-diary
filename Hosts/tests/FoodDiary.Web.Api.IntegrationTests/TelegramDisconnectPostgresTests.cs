using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class TelegramDisconnectPostgresTests(PostgresApiWebApplicationFactory factory) : IClassFixture<PostgresApiWebApplicationFactory> {
    private const string Password = "Password123!";
    private const string BotSecret = "disconnect-integration-test-secret";

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Disconnect_PersistsOwnBindingRemovalAndRevokesEveryOldSession(bool miniApp) {
        await using WebApplicationFactory<Program> app = CreateApp();
        using HttpClient client = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        string email = $"disconnect-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await client.PostAsJsonAsync("/api/v1/auth/register", new { email, password = Password, language = "en" });
        registered.EnsureSuccessStatusCode();
        JsonElement registration = await registered.Content.ReadFromJsonAsync<JsonElement>();
        Guid owner = registration["user"]["id"].GetGuid();
        long telegramId = Random.Shared.NextInt64(1_000_000, 900_000_000);
        Guid foreignId;
        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User user = await db.Users.SingleAsync(value => value.Id == new UserId(owner));
            user.SetEmailConfirmed(isConfirmed: true);
            user.LinkTelegram(telegramId);
            var foreign = User.Create($"foreign-{Guid.NewGuid():N}@example.com", "unused-hash");
            foreign.LinkTelegram(telegramId + 1);
            foreignId = foreign.Id.Value;
            db.Users.Add(foreign);
            await db.SaveChangesAsync();
        }
        using HttpClient otherDevice = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        await LoginAsync(otherDevice, email, miniApp: false);
        await LoginAsync(client, email, miniApp);
        using HttpResponseMessage beforeProfile = await client.GetAsync("/api/v1/users/overview");
        beforeProfile.EnsureSuccessStatusCode();
        JsonElement beforeOverview = await beforeProfile.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(beforeOverview["user"]["hasTelegramIdentity"].GetBoolean());
        using HttpClient bot = app.CreateClient();
        bot.DefaultRequestHeaders.Add(TelegramBotSecretAuthorizationFilter.SecretHeaderName, BotSecret);
        using HttpResponseMessage botLogin = await bot.PostAsJsonAsync("/api/v1/auth/telegram/bot/auth", new { telegramUserId = telegramId });
        botLogin.EnsureSuccessStatusCode();
        JsonElement botSession = await botLogin.Content.ReadFromJsonAsync<JsonElement>();
        string botToken = botSession.GetProperty("accessToken").GetString()!;

        using HttpClient anonymous = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using HttpResponseMessage denied = await anonymous.PostAsJsonAsync("/api/v1/auth/telegram/disconnect", new { });
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        if (!miniApp) {
            client.DefaultRequestHeaders.Remove("X-Requested-With");
            using HttpResponseMessage csrf = await client.PostAsJsonAsync("/api/v1/auth/telegram/disconnect", new { });
            Assert.Equal(HttpStatusCode.Forbidden, csrf.StatusCode);
            client.DefaultRequestHeaders.Add("X-Requested-With", "XMLHttpRequest");
            client.DefaultRequestHeaders.Add(BrowserSessionCookieService.SessionHeader, Guid.NewGuid().ToString("D"));
            using HttpResponseMessage stale = await client.PostAsJsonAsync("/api/v1/auth/telegram/disconnect", new { });
            Assert.Equal(HttpStatusCode.Unauthorized, stale.StatusCode);
            client.DefaultRequestHeaders.Remove(BrowserSessionCookieService.SessionHeader);
        }
        using HttpResponseMessage disconnected = await client.PostAsJsonAsync("/api/v1/auth/telegram/disconnect", new { userId = foreignId });
        Assert.Equal(HttpStatusCode.NoContent, disconnected.StatusCode);
        using HttpResponseMessage oldCurrent = await client.GetAsync("/api/v1/users/info");
        using HttpResponseMessage oldOther = await otherDevice.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, oldCurrent.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, oldOther.StatusCode);
        using HttpClient oldBot = app.CreateClient();
        oldBot.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botToken);
        using HttpResponseMessage oldBotAccess = await oldBot.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, oldBotAccess.StatusCode);
        using HttpResponseMessage botAfter = await bot.PostAsJsonAsync("/api/v1/auth/telegram/bot/auth", new { telegramUserId = telegramId });
        Assert.Equal(HttpStatusCode.NotFound, botAfter.StatusCode);
        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            Assert.Null((await db.Users.AsNoTracking().SingleAsync(user => user.Id == new UserId(owner))).TelegramUserId);
            Assert.Equal(telegramId + 1, (await db.Users.AsNoTracking().SingleAsync(user => user.Id == new UserId(foreignId))).TelegramUserId);
        }
        using HttpClient returning = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        JsonElement restored = await LoginAsync(returning, email, miniApp: false);
        Assert.False(restored["user"]["hasTelegramIdentity"].GetBoolean());
        using HttpResponseMessage freshProfile = await returning.GetAsync("/api/v1/users/overview");
        freshProfile.EnsureSuccessStatusCode();
        JsonElement freshOverview = await freshProfile.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(freshOverview["user"]["hasTelegramIdentity"].GetBoolean());
    }

    private WebApplicationFactory<Program> CreateApp() => factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
        configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
            ["TelegramBot:ApiSecret"] = BotSecret,
        })));

    private static async Task<JsonElement> LoginAsync(HttpClient client, string email, bool miniApp) {
        if (miniApp) {
            client.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "telegram");
        }
        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        response.EnsureSuccessStatusCode();
        JsonElement payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        if (miniApp) {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload.GetProperty("accessToken").GetString());
        }
        return payload;
    }
}
