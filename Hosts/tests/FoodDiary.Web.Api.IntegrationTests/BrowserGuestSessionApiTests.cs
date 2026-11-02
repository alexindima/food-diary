using System.Net;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class BrowserGuestSessionApiTests(TransportApiWebApplicationFactory factory) : IClassFixture<TransportApiWebApplicationFactory> {
    [Theory]
    [InlineData("http://localhost")]
    [InlineData("https://localhost")]
    public async Task GuestRestoration_WithoutCookie_ReturnsNoContentAndCannotAccessProtectedApi(string origin) {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions {
            HandleCookies = false,
            BaseAddress = new Uri(origin),
        });
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "web");
        using HttpResponseMessage restored = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.NoContent, restored.StatusCode);
        Assert.Equal(string.Empty, await restored.Content.ReadAsStringAsync());
        Assert.False(restored.Headers.Contains("Set-Cookie"));
        Assert.True(restored.Headers.CacheControl?.NoStore);

        foreach (string path in new[] { "/api/v1/users/info", "/api/v1/auth/sessions", "/api/v1/admin/users" }) {
            using HttpResponseMessage protectedResponse = await browser.GetAsync(path);
            Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
        }
    }

    [Theory]
    [InlineData("admin", null)]
    [InlineData("telegram", null)]
    [InlineData("web", BrowserSessionCookieService.SessionHeader)]
    [InlineData("web", BrowserSessionCookieService.ContextHeader)]
    public async Task MissingCookie_WithExplicitAuthenticationContext_RemainsUnauthorized(string client, string? selector) {
        using HttpClient browser = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        browser.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, client);
        if (selector is not null) {
            browser.DefaultRequestHeaders.Add(selector, Guid.NewGuid().ToString("D"));
        }
        using HttpResponseMessage response = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.False(response.Headers.Contains("Set-Cookie"));
    }
}
