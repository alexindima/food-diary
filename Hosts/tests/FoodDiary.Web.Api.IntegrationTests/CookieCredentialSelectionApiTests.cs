using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CookieCredentialSelectionApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExplicitInvalidOrRefreshBearer_CannotUseTheAccompanyingValidBrowserCookie(bool refreshToken) {
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, browser);
        string token = "not-a-valid-jwt";
        if (refreshToken) {
            await using AsyncServiceScope scope = factory.Services.CreateAsyncScope();
            token = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>().GenerateRefreshToken(new UserId(identity.Id), identity.Email, []);
        }
        browser.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using HttpResponseMessage denied = await browser.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        browser.DefaultRequestHeaders.Authorization = null;
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, identity.Id);
    }

    [RequiresDockerTheory]
    [InlineData("email-verification", "invalid")]
    [InlineData("notifications", "invalid")]
    [InlineData("food-recognition", "invalid")]
    [InlineData("notifications", "")]
    [InlineData("notifications", "%20")]
    public async Task InvalidHubQueryToken_CannotFallBackToAWorkingBrowserCookie(string hub, string token) {
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, browser);
        using HttpResponseMessage denied = await browser.PostAsync($"/hubs/{hub}/negotiate?negotiateVersion=1&access_token={token}", content: null);
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, identity.Id);
    }

    [RequiresDockerFact]
    public async Task OrdinaryLogin_RemovesTheLegacyRefreshCookieAndKeepsCredentialsOutOfTheResponse() {
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, browser);
        using HttpClient migrated = factory.CreateClient();
        AuthenticationResponseCookies.ApplyBrowserSession(migrated, identity.Cookie + "; " + RefreshTokenCookieService.CookieName + "=retired-token");
        using HttpResponseMessage login = await migrated.PostAsJsonAsync("/api/v1/auth/login", new { email = identity.Email, password = CookieAuthenticationScenario.Password });
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        string deletion = Assert.Single(login.Headers.GetValues("Set-Cookie"), value => value.StartsWith(RefreshTokenCookieService.CookieName + "=", StringComparison.Ordinal));
        Assert.Contains("expires=Thu, 01 Jan 1970", deletion, StringComparison.OrdinalIgnoreCase);
        string json = await login.Content.ReadAsStringAsync();
        Assert.DoesNotContain("accessToken", json, StringComparison.Ordinal);
        Assert.DoesNotContain("refreshToken", json, StringComparison.Ordinal);
        AuthenticationResponseCookies.ApplyBrowserSession(migrated, AuthenticationResponseCookies.ReadBrowserSession(login));
        await CookieAuthenticationScenario.AssertIdentityAsync(migrated, identity.Id);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(browser);
    }
}
