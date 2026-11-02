using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Users.Domain.Contracts.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.Privacy;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class HealthConsentApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task HealthConsent_GatesDiaryAccessWithoutEndingBrowserSessionAsync() {
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, browser);

        await AssertConsentRequiredAsync(browser);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, identity.Id);

        await browser.GrantHealthTrackingConsentAsync();
        using HttpResponseMessage allowed = await browser.GetAsync("/api/v1/meals?page=1&limit=10");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode);

        using HttpResponseMessage revoked = await browser.PutAsJsonAsync("/api/v1/users/privacy", new {
            Purpose = PrivacyConsentPurpose.HealthTracking,
            Granted = false,
            NoticeVersion = PrivacyNoticeVersions.Current,
        });
        Assert.Equal(HttpStatusCode.NoContent, revoked.StatusCode);
        await AssertConsentRequiredAsync(browser);
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, identity.Id);
    }

    private static async Task AssertConsentRequiredAsync(HttpClient browser) {
        using HttpResponseMessage response = await browser.GetAsync("/api/v1/meals?page=1&limit=10");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        JsonElement error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Privacy.HealthConsentRequired", error.GetProperty("error").GetString());
    }
}
