using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class AuthRateLimitingIntegrationTests {
    [Fact]
    public async Task TelegramQueue_OnlyVerifiedBotUsesDedicatedBudgetAsync() {
        const string endpoint = "/api/v1/auth/telegram/bot/operations/ready";
        const string botSecret = "telegram-queue-integration-test-secret";
        const string secretHeader = "X-Telegram-Bot-Secret";
        await using var factory = new TransportApiWebApplicationFactory();
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) {
                    ["TelegramBot:ApiSecret"] = botSecret,
                    ["TelegramClient:OperationsEnabled"] = "false",
                    ["RateLimiting:Auth:PermitLimit"] = "2",
                    ["RateLimiting:TelegramOperations:PermitLimit"] = "6",
                })));
        using HttpClient client = app.CreateClient();
        client.DefaultRequestHeaders.Add(secretHeader, "invalid-test-secret");

        for (int attempt = 0; attempt < 3; attempt++) {
            using HttpResponseMessage response = await client.GetAsync(endpoint);
            Assert.Equal(attempt < 2 ? HttpStatusCode.Unauthorized : HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        client.DefaultRequestHeaders.Remove(secretHeader);
        client.DefaultRequestHeaders.Add(secretHeader, botSecret);
        for (int attempt = 0; attempt < 7; attempt++) {
            using HttpResponseMessage response = await client.GetAsync(endpoint);
            Assert.Equal(attempt < 6 ? HttpStatusCode.Conflict : HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        client.DefaultRequestHeaders.Remove(secretHeader);
        for (int attempt = 0; attempt < 3; attempt++) {
            using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/auth/register", new { });
            Assert.Equal(attempt < 2 ? HttpStatusCode.BadRequest : HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    [Fact]
    public async Task LoginMethodsAndRecovery_KeepIndependentHttpBudgetsAsync() {
        await using var factory = new TransportApiWebApplicationFactory();
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) {
                    ["RateLimiting:PasswordLogin:PermitLimit"] = "2",
                    ["RateLimiting:GoogleLogin:PermitLimit"] = "2",
                    ["RateLimiting:TelegramLogin:PermitLimit"] = "2",
                    ["RateLimiting:Auth:PermitLimit"] = "2",
                })));
        using HttpClient client = app.CreateClient();
        foreach (string endpoint in new[] { "login", "google", "telegram/login-widget", "password-reset/request", "register" }) {
            for (int attempt = 0; attempt < 3; attempt++) {
                using HttpResponseMessage response = await client.PostAsJsonAsync($"/api/v1/auth/{endpoint}", new { });
                Assert.Equal(attempt < 2 ? HttpStatusCode.BadRequest : HttpStatusCode.TooManyRequests, response.StatusCode);
            }
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ConfigurationAndAuthentication_KeepIndependentBoundedQuotasAsync(bool configurationFirst) {
        await using var factory = new TransportApiWebApplicationFactory();
        await using WebApplicationFactory<Program> app = factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, configuration) =>
                configuration.AddInMemoryCollection(new Dictionary<string, string?>(StringComparer.Ordinal) {
                    ["RateLimiting:Auth:PermitLimit"] = "2",
                    ["RateLimiting:Auth:WindowSeconds"] = "60",
                    ["RateLimiting:Auth:QueueLimit"] = "0",
                    ["RateLimiting:PasswordLogin:PermitLimit"] = "2",
                })));
        using HttpClient client = app.CreateClient();

        foreach (bool configuration in new[] { configurationFirst, !configurationFirst }) {
            for (int attempt = 0; attempt < 3; attempt++) {
                // Invalid login input is rejected before database I/O, but still consumes
                // the real authentication middleware quota.
                using HttpResponseMessage response = configuration
                    ? await client.GetAsync("/api/v1/auth/telegram/configuration")
                    : await client.PostAsJsonAsync("/api/v1/auth/login", new { });
                HttpStatusCode expected = configuration ? HttpStatusCode.OK : HttpStatusCode.BadRequest;
                if (attempt == 2) {
                    expected = HttpStatusCode.TooManyRequests;
                }
                Assert.Equal(expected, response.StatusCode);
                if (attempt != 2) {
                    continue;
                }

                using var error = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                Assert.Multiple(
                    () => Assert.Equal("RateLimit.Exceeded", error.RootElement.GetProperty("error").GetString()),
                    () => Assert.False(string.IsNullOrWhiteSpace(error.RootElement.GetProperty("traceId").GetString())),
                    () => Assert.NotNull(response.Headers.RetryAfter?.Delta));
                Assert.InRange(response.Headers.RetryAfter!.Delta!.Value.TotalSeconds, 1, 60);
            }
        }
    }
}
