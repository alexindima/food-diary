using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class TelegramPremiumTrialPostgresTests(PostgresApiWebApplicationFactory factory) : IClassFixture<PostgresApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task ConcurrentTelegramTrialRequests_GrantExactlyOneTrial() {
        const string secret = "telegram-trial-integration-secret";
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder => builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                ["TelegramBot:ApiSecret"] = secret,
            })));
        long telegramUserId = Random.Shared.NextInt64(1_000_000, 900_000_000);
        await using (AsyncServiceScope scope = app.Services.CreateAsyncScope()) {
            FoodDiaryDbContext db = scope.ServiceProvider.GetRequiredService<FoodDiaryDbContext>();
            db.Users.Add(User.CreateTelegram(telegramUserId, "inaccessible-test-hash"));
            await db.SaveChangesAsync();
        }

        using HttpClient client = app.CreateClient();
        client.DefaultRequestHeaders.Add(TelegramBotSecretAuthorizationFilter.SecretHeaderName, secret);
        using HttpResponseMessage login = await client.PostAsJsonAsync("/api/v1/auth/telegram/bot/auth", new { telegramUserId });
        login.EnsureSuccessStatusCode();
        JsonElement session = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());

        HttpResponseMessage[] attempts = await Task.WhenAll(client.PostAsync("/api/v1/billing/trial", content: null),
            client.PostAsync("/api/v1/billing/trial", content: null));
        using HttpResponseMessage first = attempts[0];
        using HttpResponseMessage second = attempts[1];
        Assert.Single(attempts, attempt => attempt.StatusCode == HttpStatusCode.OK);
        HttpResponseMessage rejected = Assert.Single(attempts, attempt => attempt.StatusCode == HttpStatusCode.Conflict);
        JsonElement error = await rejected.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Billing.TrialAlreadyUsed", error.GetProperty("error").GetString());
    }
}
