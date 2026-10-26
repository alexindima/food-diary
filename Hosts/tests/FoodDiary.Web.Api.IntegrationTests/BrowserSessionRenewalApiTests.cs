using System.Globalization;
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

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserSessionRenewalApiTests(CookieExpiryApiWebApplicationFactory factory) : IClassFixture<CookieExpiryApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false, 30)]
    [InlineData(true, 90)]
    public async Task ActiveBrowser_RenewsExpiryAndCookie_WithoutChangingCredential(bool rememberMe, int days) {
        factory.Clock.UtcNow = DateTimeOffset.FromUnixTimeSeconds(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        string email = $"renewal-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, "Password123!", "en"));
        registered.EnsureSuccessStatusCode();
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, "Password123!", rememberMe));
        login.EnsureSuccessStatusCode();
        JsonElement payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        var id = new RefreshTokenSessionId(Guid.Parse(payload.GetProperty("sessionId").GetString()!));
        UserRefreshTokenSession original = await ReadSessionAsync(id);
        DateTimeOffset expires = new(original.ExpiresAtUtc!.Value);
        factory.Clock.UtcNow = expires.AddDays(-7).AddSeconds(-1);
        using HttpResponseMessage early = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, early.StatusCode);
        Assert.False(early.Headers.Contains("Set-Cookie"));
        factory.Clock.UtcNow = expires.AddDays(-7);
        using HttpResponseMessage renewed = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, renewed.StatusCode);
        UserRefreshTokenSession current = await ReadSessionAsync(id);
        Assert.True(current.ExpiresAtUtc == factory.Clock.UtcNow.UtcDateTime.AddDays(days),
            "Active ordinary browser sessions must extend their expiry in the renewal window.");
        Assert.Equal(original.RefreshTokenHash, current.RefreshTokenHash);
        string cookie = Assert.Single(renewed.Headers.GetValues("Set-Cookie"), value => value.StartsWith("fooddiary.browser=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(rememberMe, cookie.Contains("expires=", StringComparison.OrdinalIgnoreCase));
        if (rememberMe) {
            Assert.Contains(current.ExpiresAtUtc!.Value.ToString("R", CultureInfo.InvariantCulture), cookie, StringComparison.Ordinal);
        }
        factory.Clock.UtcNow = expires.AddSeconds(1);
        using HttpResponseMessage afterOriginalExpiry = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, afterOriginalExpiry.StatusCode);
        using HttpResponseMessage logout = await browser.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using HttpResponseMessage afterLogout = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, afterLogout.StatusCode);
    }

    private async Task<UserRefreshTokenSession> ReadSessionAsync(RefreshTokenSessionId id) {
        await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
        return Assert.IsType<UserRefreshTokenSession>(await scope.ServiceProvider.GetRequiredService<IRefreshTokenSessionWriteRepository>().GetByIdAsync(id));
    }
}
