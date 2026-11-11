using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Meals.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Entities;
using FoodDiary.Modules.Products.Domain.Contracts.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Recipes.Domain.Entities;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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
    [RequiresDockerTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task ExistingEmail_CombinesOnlyEligibleDiaries(bool useTelegramGoals, bool targetIsAdmin) {
        const string password = "Password123!";
        const string botSecret = "telegram-transfer-integration-secret";
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["TelegramClient:LoginEnabled"] = "true",
                ["TelegramClient:RegistrationEnabled"] = "true",
                ["TelegramBot:ApiSecret"] = botSecret,
                ["TelegramAuth:BotToken"] = "123:test-token",
            })));
        await CompleteTelegramCutoverForScenarioAsync(app);
        using HttpClient source = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        source.DefaultRequestHeaders.Add("X-FoodDiary-Client", "telegram");
        long telegramId = Random.Shared.NextInt64(1_000_000, 900_000_000);
        using HttpResponseMessage begin = await source.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin", new { InitData = CreateInitData(telegramId) });
        begin.EnsureSuccessStatusCode();
        JsonElement intent = await begin.Content.ReadFromJsonAsync<JsonElement>();
        using HttpResponseMessage created = await source.PostAsJsonAsync("/api/v1/auth/telegram/complete",
            new { Ticket = intent.GetProperty("ticket").GetString(), Action = "register", Language = "en", TimeZoneId = "UTC" });
        Assert.True(created.IsSuccessStatusCode, created.IsSuccessStatusCode ? null : await created.Content.ReadAsStringAsync());
        JsonElement sourceSession = await created.Content.ReadFromJsonAsync<JsonElement>();
        var sourceId = new UserId(sourceSession.GetProperty("user").GetProperty("id").GetGuid());
        string? sourceAccess = sourceSession.GetProperty("accessToken").GetString();
        source.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sourceAccess);
        using HttpResponseMessage startedTrial = await source.PostAsync("/api/v1/billing/trial", content: null);
        startedTrial.EnsureSuccessStatusCode();
        JsonElement trial = await startedTrial.Content.ReadFromJsonAsync<JsonElement>();
        DateTime trialStartedAt = trial.GetProperty("premiumTrialStartUtc").GetDateTime();
        DateTime trialEndsAt = trial.GetProperty("premiumTrialEndUtc").GetDateTime();
        Assert.Equal(TimeSpan.FromDays(7), trialEndsAt - trialStartedAt);

        using HttpClient target = app.CreateClient();
        string email = $"telegram-transfer-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await target.PostAsJsonAsync("/api/v1/auth/register", new { email, password, language = "en" });
        registered.EnsureSuccessStatusCode();
        JsonElement targetSession = await registered.Content.ReadFromJsonAsync<JsonElement>();
        var targetId = new UserId(targetSession.GetProperty("user").GetProperty("id").GetGuid());
        using HttpResponseMessage verified = await target.PostAsJsonAsync("/api/v1/auth/verify-email",
            new { UserId = targetId.Value, Token = factory.EmailSender.GetRequiredEmailVerificationToken(email) });
        Assert.Equal(HttpStatusCode.NoContent, verified.StatusCode);

        var sourceMeal = Meal.Create(sourceId, DateTime.UtcNow, comment: "Telegram meal");
        var targetMeal = Meal.Create(targetId, DateTime.UtcNow, comment: "Email meal");
        var recipe = Recipe.Create(sourceId, "Transfer recipe", servings: 2);
        var product = Product.Create(sourceId, "Transfer product", MeasurementUnit.G, baseAmount: 100,
            defaultPortionAmount: 100, caloriesPerBase: 50, proteinsPerBase: 1, fatsPerBase: 0,
            carbsPerBase: 10, fiberPerBase: 1, alcoholPerBase: 0);
        sourceMeal.AddProduct(product.Id, ProductUnitQuantity.FromUnits(100));
        sourceMeal.AddRecipe(recipe.Id, RecipeServingQuantity.FromServings(1));
        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            (await db.Users.SingleAsync(item => item.Id == targetId)).UpdateGoals(dailyCalorieTarget: 2300);
            db.AddRange(product, recipe, sourceMeal, targetMeal);
            await db.SaveChangesAsync();
            if (targetIsAdmin) {
                IUserRoleMembershipService roles = scope.ServiceProvider.GetRequiredService<IUserRoleMembershipService>();
                await roles.EnsureRoleAsync(targetId, RoleNames.Admin);
            }
        }

        using HttpResponseMessage request = await source.PostAsJsonAsync("/api/v1/auth/telegram/backup-email",
            new { Email = email, InitData = CreateInitData(telegramId) });
        Assert.Equal(HttpStatusCode.NoContent, request.StatusCode);
        using HttpResponseMessage confirmation = await source.PostAsJsonAsync("/api/v1/auth/telegram/backup-email/verify",
            new { UserId = sourceId.Value, Token = factory.EmailSender.GetRequiredEmailVerificationToken(email) });
        confirmation.EnsureSuccessStatusCode();
        JsonElement verification = await confirmation.Content.ReadFromJsonAsync<JsonElement>();
        string ticket = verification.GetProperty("transferTicket").GetString()!;
        const string transferRoute = "/api/v1/auth/telegram/diary-transfer";
        using HttpResponseMessage wrongAccount = await source.PostAsJsonAsync(transferRoute, new { ticket, useTelegramGoals });
        Assert.Equal(HttpStatusCode.Forbidden, wrongAccount.StatusCode);
        using HttpResponseMessage stillSource = await source.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, stillSource.StatusCode);

        source.DefaultRequestHeaders.Authorization = null;
        using HttpResponseMessage signedIn = await source.PostAsJsonAsync("/api/v1/auth/login", new { email, password });
        signedIn.EnsureSuccessStatusCode();
        JsonElement login = await signedIn.Content.ReadFromJsonAsync<JsonElement>();
        string? targetAccess = login.GetProperty("accessToken").GetString();
        source.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetAccess);
        using HttpClient otherBrowser = app.CreateClient();
        otherBrowser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", targetAccess);
        using HttpResponseMessage unbound = await otherBrowser.PostAsJsonAsync(transferRoute, new { ticket, useTelegramGoals });
        Assert.Equal(HttpStatusCode.Conflict, unbound.StatusCode);

        if (targetIsAdmin) {
            await using (AsyncServiceScope scope = app.Services.CreateAsyncScope()) {
                FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
                DateTime staleAuthentication = DateTime.UtcNow.AddMinutes(-16);
                int aged = await db.Set<UserRefreshTokenSession>()
                    .Where(session => session.UserId == targetId && session.AuthProvider == "password")
                    .ExecuteUpdateAsync(setters => setters.SetProperty(session => session.CreatedAtUtc, staleAuthentication));
                Assert.Equal(1, aged);
            }
            using HttpResponseMessage expiredConfirmation = await source.PostAsJsonAsync(transferRoute, new { ticket });
            await AssertConfirmationRequiredAsync(expiredConfirmation);
            using HttpResponseMessage restored = await source.PostAsJsonAsync("/api/v1/auth/refresh", new { });
            restored.EnsureSuccessStatusCode();
            JsonElement refreshed = await restored.Content.ReadFromJsonAsync<JsonElement>();
            source.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.GetProperty("accessToken").GetString());
            using HttpResponseMessage stillStale = await source.PostAsJsonAsync(transferRoute, new { ticket });
            await AssertConfirmationRequiredAsync(stillStale);

            using HttpResponseMessage wrongConfirmation = await source.PostAsJsonAsync("/api/v1/auth/login",
                new { email, password, ExpectedUserId = sourceId.Value });
            Assert.Equal(HttpStatusCode.Forbidden, wrongConfirmation.StatusCode);
            Assert.False(wrongConfirmation.Headers.Contains("Set-Cookie"));
            using HttpResponseMessage confirmedLogin = await source.PostAsJsonAsync("/api/v1/auth/login",
                new { email, password, ExpectedUserId = targetId.Value });
            confirmedLogin.EnsureSuccessStatusCode();
            JsonElement confirmedSession = await confirmedLogin.Content.ReadFromJsonAsync<JsonElement>();
            source.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", confirmedSession.GetProperty("accessToken").GetString());
        }

        using HttpResponseMessage previewResponse = await source.PostAsJsonAsync(transferRoute, new { ticket });
        Assert.True(previewResponse.IsSuccessStatusCode, await previewResponse.Content.ReadAsStringAsync());
        JsonElement preview = await previewResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, preview.GetProperty("counts").GetProperty("meals").GetInt32());
        Assert.Equal(1, preview.GetProperty("counts").GetProperty("recipes").GetInt32());
        Assert.Equal(1, preview.GetProperty("counts").GetProperty("products").GetInt32());
        Assert.Equal(2000, preview.GetProperty("telegramGoals").GetProperty("calories").GetDouble());
        Assert.Equal(2300, preview.GetProperty("emailGoals").GetProperty("calories").GetDouble());
        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            Assert.Equal(sourceId, (await db.Meals.SingleAsync(item => item.Id == sourceMeal.Id)).UserId);
            Assert.Null((await db.Users.SingleAsync(item => item.Id == sourceId)).DeletedAt);
        }

        using HttpResponseMessage missingConsent = await source.PostAsJsonAsync(transferRoute, new { ticket, useTelegramGoals });
        Assert.Equal(HttpStatusCode.Forbidden, missingConsent.StatusCode);
        await source.GrantHealthTrackingConsentAsync();
        using HttpResponseMessage confirmed = await source.PostAsJsonAsync(transferRoute, new { ticket, useTelegramGoals });
        Assert.True(confirmed.IsSuccessStatusCode, await confirmed.Content.ReadAsStringAsync());
        using HttpResponseMessage staleTarget = await source.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, staleTarget.StatusCode);
        source.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", sourceAccess);
        using HttpResponseMessage staleSource = await source.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, staleSource.StatusCode);

        using HttpClient bot = app.CreateClient();
        bot.DefaultRequestHeaders.Add(TelegramBotSecretAuthorizationFilter.SecretHeaderName, botSecret);
        using HttpResponseMessage botAuth = await bot.PostAsJsonAsync("/api/v1/auth/telegram/bot/auth", new { telegramUserId = telegramId });
        botAuth.EnsureSuccessStatusCode();
        JsonElement botSession = await botAuth.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(targetId.Value, botSession.GetProperty("user").GetProperty("id").GetGuid());
        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            User old = await db.Users.SingleAsync(item => item.Id == sourceId);
            Assert.NotNull(old.DeletedAt);
            Assert.Null(old.TelegramUserId);
            User combined = await db.Users.SingleAsync(item => item.Id == targetId);
            Assert.Equal(useTelegramGoals ? 2000 : 2300, combined.DailyCalorieTarget);
            Assert.Equal(trialStartedAt.Ticks / TimeSpan.TicksPerMicrosecond, combined.PremiumTrialStartedAtUtc!.Value.Ticks / TimeSpan.TicksPerMicrosecond);
            Assert.Equal(trialEndsAt.Ticks / TimeSpan.TicksPerMicrosecond, combined.PremiumTrialEndsAtUtc!.Value.Ticks / TimeSpan.TicksPerMicrosecond);
            Assert.Equal(2, await db.Meals.CountAsync(item => item.UserId == targetId));
            Assert.Equal(2, await db.MealItems.CountAsync(item => item.MealId == sourceMeal.Id));
            Assert.Equal(targetId, (await db.Products.SingleAsync(item => item.Id == product.Id)).UserId);
            Assert.Equal(targetId, (await db.Recipes.SingleAsync(item => item.Id == recipe.Id)).UserId);
        }

        source.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", botSession.GetProperty("accessToken").GetString());
        if (targetIsAdmin) {
            using HttpResponseMessage diary = await source.GetAsync("/api/v1/users/info");
            diary.EnsureSuccessStatusCode();
            using HttpResponseMessage admin = await source.GetAsync("/api/v1/admin/users");
            await AssertConfirmationRequiredAsync(admin);
            using HttpResponseMessage adminSso = await source.PostAsync("/api/v1/auth/admin-sso/start", content: null);
            await AssertConfirmationRequiredAsync(adminSso);
            using HttpResponseMessage setPassword = await source.PatchAsJsonAsync("/api/v1/users/password/set", new { NewPassword = password });
            await AssertConfirmationRequiredAsync(setPassword);
        }
        using HttpResponseMessage unlinked = await source.PostAsJsonAsync("/api/v1/auth/telegram/unlink", new { InitData = CreateInitData(telegramId) });
        Assert.Equal(HttpStatusCode.NoContent, unlinked.StatusCode);

        using HttpClient recreated = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        recreated.DefaultRequestHeaders.Add("X-FoodDiary-Client", "telegram");
        using HttpResponseMessage newBegin = await recreated.PostAsJsonAsync("/api/v1/auth/telegram/mini-app/begin", new { InitData = CreateInitData(telegramId) });
        newBegin.EnsureSuccessStatusCode();
        JsonElement newIntent = await newBegin.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("onboarding", newIntent.GetProperty("nextAction").GetString());
        using HttpResponseMessage newRegistration = await recreated.PostAsJsonAsync("/api/v1/auth/telegram/complete",
            new { Ticket = newIntent.GetProperty("ticket").GetString(), Action = "register", Language = "en", TimeZoneId = "UTC" });
        newRegistration.EnsureSuccessStatusCode();
        JsonElement newSession = await newRegistration.Content.ReadFromJsonAsync<JsonElement>();
        Guid newUserId = newSession.GetProperty("user").GetProperty("id").GetGuid();
        Assert.NotEqual(sourceId.Value, newUserId);
        Assert.NotEqual(targetId.Value, newUserId);
        recreated.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", newSession.GetProperty("accessToken").GetString());

        using HttpResponseMessage repeatedTrial = await recreated.PostAsync("/api/v1/billing/trial", content: null);
        Assert.Equal(HttpStatusCode.Conflict, repeatedTrial.StatusCode);
        JsonElement trialError = await repeatedTrial.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Billing.TrialAlreadyUsed", trialError.GetProperty("error").GetString());
        using HttpResponseMessage overview = await recreated.GetAsync("/api/v1/billing/overview");
        overview.EnsureSuccessStatusCode();
        JsonElement eligibility = await overview.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Multiple(
            () => Assert.False(eligibility.GetProperty("canStartPremiumTrial").GetBoolean()),
            () => Assert.True(eligibility.GetProperty("premiumTrialUsed").GetBoolean()),
            () => Assert.False(eligibility.GetProperty("premiumTrialActive").GetBoolean()),
            () => Assert.Equal(JsonValueKind.Null, eligibility.GetProperty("premiumTrialStartUtc").ValueKind));
    }

    private static async Task AssertConfirmationRequiredAsync(HttpResponseMessage response) {
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        JsonElement error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Authentication.ReauthenticationRequired", error.GetProperty("error").GetString());
    }

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
        using HttpClient client = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-FoodDiary-Client", "telegram");
        using HttpResponseMessage quarantined = await client.PostAsJsonAsync(
            "/api/v1/auth/telegram/mini-app/begin", new { InitData = CreateInitData() });
        Assert.Equal(HttpStatusCode.Unauthorized, quarantined.StatusCode);
        await CompleteTelegramCutoverForScenarioAsync(app);
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
        using HttpClient otherUserClient = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
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

    private static async Task CompleteTelegramCutoverForScenarioAsync(WebApplicationFactory<Program> app) {
        // This isolated happy-path fixture starts after the migration quarantine.
        // The request above proves the pre-cutover restriction before advancing its test-only epoch.
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        FoodDiaryDbContext context = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
        int updated = await context.Database.ExecuteSqlRawAsync(
            "UPDATE \"TelegramAssertionPolicies\" SET \"NotBeforeUtc\" = date_trunc('second', clock_timestamp()) - INTERVAL '1 second' WHERE \"Id\" = 1");
        Assert.Equal(1, updated);
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

    private static string CreateInitData(long telegramUserId = 987654321) {
        string authDate = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        string queryId = Guid.NewGuid().ToString("N");
        string user = JsonSerializer.Serialize(new { id = telegramUserId, first_name = "Test", language_code = "en" });
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
        using HttpClient client = app.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        client.DefaultRequestHeaders.Add("X-FoodDiary-Client", "telegram");
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
        bool tokenMode = authentication.TryGetProperty("accessToken", out JsonElement token);
        client.DefaultRequestHeaders.Authorization = tokenMode ? new AuthenticationHeaderValue("Bearer", token.GetString()) : null;
        using HttpResponseMessage profile = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        using var profileBody = JsonDocument.Parse(await profile.Content.ReadAsStringAsync());
        Assert.Equal(expectedEmail, profileBody.RootElement.GetProperty("email").GetString());
        using HttpResponseMessage refresh = await client.PostAsJsonAsync(tokenMode ? "/api/v1/auth/refresh" : "/api/v1/auth/session",
            new { });
        Assert.Equal(HttpStatusCode.OK, refresh.StatusCode);
        using var refreshed = JsonDocument.Parse(await refresh.Content.ReadAsStringAsync());
        if (tokenMode) {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshed.RootElement.GetProperty("accessToken").GetString());
        }
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
