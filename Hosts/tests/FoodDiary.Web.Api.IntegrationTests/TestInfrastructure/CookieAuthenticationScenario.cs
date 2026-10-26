using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class CookieAuthenticationScenario {
    public const string Password = "CookieTestPassword123!";

    public static HttpClient CreateBrowser(PostgresApiWebApplicationFactory factory, string origin = "http://localhost") =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, BaseAddress = new Uri(origin) });

    public static async Task<CookieIdentity> RegisterAsync(PostgresApiWebApplicationFactory factory, HttpClient browser) {
        string email = $"cookie-access-{Guid.NewGuid():N}@example.com";
        using HttpResponseMessage registered = await browser.PostAsJsonAsync("/api/v1/auth/register", new RegisterHttpRequest(email, Password, "en"));
        Assert.Equal(HttpStatusCode.OK, registered.StatusCode);
        JsonElement payload = await registered.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(payload.TryGetProperty("accessToken", out _));
        Assert.False(payload.TryGetProperty("refreshToken", out _));
        Guid id = payload.GetProperty("user").GetProperty("id").GetGuid();
        string cookie = AuthenticationResponseCookies.ReadBrowserSession(registered);
        using HttpResponseMessage confirmed = await browser.PostAsJsonAsync("/api/v1/auth/verify-email",
            new { UserId = id, Token = factory.EmailSender.GetRequiredEmailVerificationToken(email) });
        Assert.Equal(HttpStatusCode.NoContent, confirmed.StatusCode);
        using HttpResponseMessage restored = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        return new CookieIdentity(id, email, cookie);
    }

    public static async Task LoginAsync(HttpClient browser, string email, string password = Password) {
        using HttpResponseMessage login = await browser.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(email, password));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        JsonElement payload = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(payload.TryGetProperty("accessToken", out _));
        Assert.False(payload.TryGetProperty("refreshToken", out _));
    }

    public static async Task<CookieIdentity> CreateAdministratorAsync(PostgresApiWebApplicationFactory factory, HttpClient browser) {
        CookieIdentity identity = await RegisterAsync(factory, browser);
        // Bootstrap only the persisted role. Authentication and SSO still run through the real HTTP pipeline.
        await using (AsyncServiceScope scope = factory.Services.CreateAsyncScope()) {
            IUserRoleMembershipService roles = scope.ServiceProvider.GetRequiredService<IUserRoleMembershipService>();
            await roles.EnsureRoleAsync(new UserId(identity.Id), RoleNames.Admin);
        }
        await LoginAsync(browser, identity.Email);
        using HttpResponseMessage start = await browser.PostAsync("/api/v1/auth/admin-sso/start", content: null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        JsonElement handoff = await start.Content.ReadFromJsonAsync<JsonElement>();
        using HttpResponseMessage exchange = await browser.PostAsJsonAsync("/api/v1/auth/admin-sso/exchange",
            new { Code = handoff.GetProperty("code").GetString() });
        Assert.Equal(HttpStatusCode.OK, exchange.StatusCode);
        Assert.Contains("fooddiary.admin=", AuthenticationResponseCookies.ReadBrowserSession(exchange), StringComparison.Ordinal);
        return identity;
    }

    public static async Task AssertIdentityAsync(HttpClient browser, Guid id) {
        using HttpResponseMessage profile = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.OK, profile.StatusCode);
        JsonElement payload = await profile.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, payload.GetProperty("id").GetGuid());
    }

    public static async Task AssertRejectedSessionAsync(HttpClient browser) {
        using HttpResponseMessage profile = await browser.GetAsync("/api/v1/users/info");
        using HttpResponseMessage restore = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, restore.StatusCode);
    }
}
