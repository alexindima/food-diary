using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.Entities.Users;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionRenewalBoundaryApiTests(CookieExpiryApiWebApplicationFactory factory) : IClassFixture<CookieExpiryApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task RepeatedActivity_HasNoDefaultAbsoluteLimit() {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        RefreshTokenSessionId id = await SignInAsync(browser, rememberMe: true);
        UserRefreshTokenSession original = await ReadSessionAsync(id);
        foreach (int day in new[] { 83, 166, 249, 332, 415, 498 }) {
            factory.Clock.UtcNow = new DateTimeOffset(original.CreatedAtUtc.AddDays(day));
            using HttpResponseMessage restored = await browser.PostAsync("/api/v1/auth/session", content: null);
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
            Assert.Equal(original.CreatedAtUtc.AddDays(day + 90), (await ReadSessionAsync(id)).ExpiresAtUtc);
            Assert.Contains(restored.Headers.GetValues("Set-Cookie"), cookie => cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        }
        Assert.Equal(original.RefreshTokenHash, (await ReadSessionAsync(id)).RefreshTokenHash);
    }

    [RequiresDockerFact]
    public async Task ConfiguredAbsoluteLimit_StillCapsOrdinarySessionRenewal() {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        await using WebApplicationFactory<Program> capped = factory.WithWebHostBuilder(builder => {
            builder.UseSetting("BrowserSessions:AbsoluteLifetimeDays", "180");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>(StringComparer.Ordinal) { ["BrowserSessions:AbsoluteLifetimeDays"] = "180" }));
        });
        using HttpClient browser = capped.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        RefreshTokenSessionId id = await SignInAsync(browser, rememberMe: true);
        UserRefreshTokenSession original = await ReadSessionAsync(id);
        DateTime created = original.CreatedAtUtc;
        factory.Clock.UtcNow = new DateTimeOffset(created.AddDays(83));
        using HttpResponseMessage first = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(created.AddDays(173), (await ReadSessionAsync(id)).ExpiresAtUtc);
        factory.Clock.UtcNow = new DateTimeOffset(created.AddDays(166));
        using HttpResponseMessage second = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(created.AddDays(180), (await ReadSessionAsync(id)).ExpiresAtUtc);
        factory.Clock.UtcNow = new DateTimeOffset(created.AddDays(179));
        using HttpResponseMessage finalDay = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, finalDay.StatusCode);
        Assert.False(finalDay.Headers.Contains("Set-Cookie"));
        factory.Clock.UtcNow = new DateTimeOffset(created.AddDays(180));
        using HttpResponseMessage expired = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        using HttpResponseMessage restore = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, restore.StatusCode);
        Assert.False(restore.Headers.Contains("Set-Cookie"));
        Assert.Equal(original.RefreshTokenHash, (await ReadSessionAsync(id)).RefreshTokenHash);
    }

    [RequiresDockerFact]
    public async Task ConcurrentTabs_RenewTheSameProof_AndIdleExpiryCannotBeRevived() {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        RefreshTokenSessionId id = await SignInAsync(browser, rememberMe: false);
        UserRefreshTokenSession original = await ReadSessionAsync(id);
        factory.Clock.UtcNow = new DateTimeOffset(original.CreatedAtUtc.AddDays(24));
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 8)
            .Select(_ => browser.PostAsync("/api/v1/auth/session", content: null)));
        try {
            Assert.All(responses, response => Assert.Equal(HttpStatusCode.OK, response.StatusCode));
        } finally {
            foreach (HttpResponseMessage response in responses) { response.Dispose(); }
        }
        UserRefreshTokenSession renewed = await ReadSessionAsync(id);
        Assert.Equal(factory.Clock.UtcNow.UtcDateTime.AddDays(30), renewed.ExpiresAtUtc);
        Assert.Equal(original.RefreshTokenHash, renewed.RefreshTokenHash);
        factory.Clock.UtcNow = new DateTimeOffset(renewed.ExpiresAtUtc!.Value);
        using HttpResponseMessage expired = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, expired.StatusCode);
        Assert.Equal(renewed.ExpiresAtUtc, (await ReadSessionAsync(id)).ExpiresAtUtc);
    }

    private static async Task<RefreshTokenSessionId> SignInAsync(HttpClient browser, bool rememberMe) {
        string email = $"renewal-boundary-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage register = await browser.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        register.EnsureSuccessStatusCode();
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!", rememberMe));
        login.EnsureSuccessStatusCode();
        JsonElement response = await login.Content.ReadFromJsonAsync<JsonElement>();
        return new RefreshTokenSessionId(Guid.Parse(response.GetProperty("sessionId").GetString()!));
    }

    private async Task<UserRefreshTokenSession> ReadSessionAsync(RefreshTokenSessionId id) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        return Assert.IsType<UserRefreshTokenSession>(await scope.ServiceProvider.GetRequiredService<IRefreshTokenSessionWriteRepository>().GetByIdAsync(id));
    }
}
