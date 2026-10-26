using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Meals.Presentation.Requests;
using FoodDiary.Modules.Products.Presentation.Requests;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CookieAuthorizationApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerTheory]
    [InlineData("altered-secret")]
    [InlineData("unknown-session")]
    [InlineData("metadata-only")]
    public async Task InvalidCredentialOrPublicMetadata_CannotAuthenticateOrRestoreAUser(string attack) {
        using HttpClient owner = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, owner);
        using HttpClient attacker = factory.CreateClient();
        string[] cookie = identity.Cookie.Split('=', 2);
        string credential = cookie[1];
        if (string.Equals(attack, "altered-secret", StringComparison.Ordinal)) {
            credential = credential[..^1] + (credential[^1] == 'A' ? 'B' : 'A');
        } else if (string.Equals(attack, "unknown-session", StringComparison.Ordinal)) {
            credential = Guid.NewGuid().ToString("N") + credential[32..];
        } else {
            credential = identity.Id.ToString("D");
        }
        AuthenticationResponseCookies.ApplyBrowserSession(attacker, cookie[0] + "=" + credential);
        attacker.DefaultRequestHeaders.Add("X-Test-Auth", "true");
        attacker.DefaultRequestHeaders.Add("X-Test-UserId", identity.Id.ToString("D"));
        attacker.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(attacker);
        await CookieAuthenticationScenario.AssertIdentityAsync(owner, identity.Id);
    }

    [RequiresDockerFact]
    public async Task ForeignSessionId_CannotRevokeAnotherUsersSession() {
        using HttpClient owner = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient stranger = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity identity = await CookieAuthenticationScenario.RegisterAsync(factory, owner);
        CookieIdentity other = await CookieAuthenticationScenario.RegisterAsync(factory, stranger);
        using HttpResponseMessage restored = await owner.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        JsonElement session = await restored.Content.ReadFromJsonAsync<JsonElement>();
        using HttpResponseMessage revoke = await stranger.DeleteAsync($"/api/v1/auth/sessions/{session.GetProperty("sessionId").GetString()}");
        // Revocation deliberately has an idempotent response without disclosing foreign session existence.
        Assert.Equal(HttpStatusCode.NoContent, revoke.StatusCode);
        await CookieAuthenticationScenario.AssertIdentityAsync(owner, identity.Id);
        await CookieAuthenticationScenario.AssertIdentityAsync(stranger, other.Id);
    }

    [RequiresDockerFact]
    public async Task OrdinaryBrowser_CannotObtainAdministrativeSessionOrUseWebsiteCookieOnAdminRoutes() {
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity user = await CookieAuthenticationScenario.RegisterAsync(factory, browser);

        using HttpResponseMessage admin = await browser.GetAsync("/api/v1/admin/users");
        using HttpResponseMessage sso = await browser.PostAsync("/api/v1/auth/admin-sso/start", content: null);
        using HttpResponseMessage forgedExchange = await browser.PostAsJsonAsync("/api/v1/auth/admin-sso/exchange", new { Code = new string('x', 43) });

        Assert.Equal(HttpStatusCode.Unauthorized, admin.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, sso.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, forgedExchange.StatusCode);
        Assert.False(sso.Headers.Contains("Set-Cookie"));
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, user.Id);
    }

    [RequiresDockerTheory]
    [InlineData("GET", "/api/v1/admin/users")]
    [InlineData("PATCH", "/api/v1/admin/users/{id}")]
    [InlineData("PATCH", "/api/v1/admin/users/{id}/password")]
    [InlineData("POST", "/api/v1/admin/users/{id}/impersonation")]
    public async Task RelabeledCookieAndSpoofedHeaders_CannotGiveAnOrdinaryUserAdministrativeRights(string method, string path) {
        using HttpClient browser = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity user = await CookieAuthenticationScenario.RegisterAsync(factory, browser);
        using HttpClient attacker = factory.CreateClient();
        // Even a valid user credential placed under the administrative cookie name must still enforce roles.
        AuthenticationResponseCookies.ApplyBrowserSession(attacker,
            user.Cookie.Replace(BrowserSessionCookieService.BrowserCookieName, BrowserSessionCookieService.AdminCookieName, StringComparison.Ordinal));
        attacker.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "admin");
        attacker.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        attacker.DefaultRequestHeaders.Add("X-Test-UserId", Guid.NewGuid().ToString("D"));
        using var request = new HttpRequestMessage(new HttpMethod(method), path.Replace("{id}", user.Id.ToString("D"), StringComparison.Ordinal)) {
            Content = string.Equals(method, "GET", StringComparison.Ordinal) ? null : JsonContent.Create(new { roles = new[] { "Admin" }, newPassword = "ReplacementPassword456!", reason = "Attempted elevation" }),
        };
        using HttpResponseMessage response = await attacker.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        using HttpResponseMessage restored = await browser.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        JsonElement session = await restored.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(session.GetProperty("roles").EnumerateArray(), role => string.Equals(role.GetString(), "Admin", StringComparison.Ordinal));
        await CookieAuthenticationScenario.AssertIdentityAsync(browser, user.Id);
    }

    [RequiresDockerTheory]
    [InlineData("read")]
    [InlineData("update")]
    [InlineData("delete")]
    [InlineData("repeat")]
    public async Task ForeignMealId_CannotExposeChangeDeleteOrCopyAnotherUsersDiary(string operation) {
        using HttpClient owner = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient stranger = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity ownerIdentity = await CookieAuthenticationScenario.RegisterAsync(factory, owner);
        CookieIdentity strangerIdentity = await CookieAuthenticationScenario.RegisterAsync(factory, stranger);
        Guid productId = await CreatePrivateProductAsync(owner);
        DateTime date = new(2026, 3, 26, 12, 0, 0, DateTimeKind.Utc);
        using HttpResponseMessage created = await owner.PostAsJsonAsync("/api/v1/meals",
            new CreateMealHttpRequest(date, "Lunch", "Private owner comment", ImageUrl: null, ImageAssetId: null, [new MealItemHttpRequest(productId, RecipeId: null, 100)]));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        JsonElement meal = await created.Content.ReadFromJsonAsync<JsonElement>();
        Guid mealId = meal.GetProperty("id").GetGuid();
        string path = $"/api/v1/meals/{mealId}";
        using HttpResponseMessage forbidden = operation switch {
            "read" => await stranger.GetAsync(path),
            "update" => await stranger.PatchAsJsonAsync(path,
                new UpdateMealHttpRequest(date, "Dinner", "Attacker comment", ImageUrl: null, ImageAssetId: null, [new MealItemHttpRequest(productId, RecipeId: null, 100)])),
            "delete" => await stranger.DeleteAsync(path),
            _ => await stranger.PostAsJsonAsync(path + "/repeat", new RepeatMealHttpRequest(date.AddDays(1), "Dinner")),
        };
        Assert.Equal(HttpStatusCode.NotFound, forbidden.StatusCode);
        Assert.DoesNotContain("Private owner comment", await forbidden.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        using HttpResponseMessage unchanged = await owner.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
        JsonElement current = await unchanged.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Private owner comment", current.GetProperty("comment").GetString());
        Assert.Equal("Lunch", current.GetProperty("mealType").GetString());
        using HttpResponseMessage strangersMeals = await stranger.GetAsync("/api/v1/meals?page=1&limit=10");
        Assert.Equal(HttpStatusCode.OK, strangersMeals.StatusCode);
        JsonElement strangersDiary = await strangersMeals.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Empty(strangersDiary.GetProperty("data").EnumerateArray());
        await CookieAuthenticationScenario.AssertIdentityAsync(owner, ownerIdentity.Id);
        await CookieAuthenticationScenario.AssertIdentityAsync(stranger, strangerIdentity.Id);
    }

    [RequiresDockerFact]
    public async Task ForeignPrivateProduct_CannotBeReadUpdatedOrDeleted() {
        using HttpClient owner = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient stranger = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.RegisterAsync(factory, owner);
        await CookieAuthenticationScenario.RegisterAsync(factory, stranger);
        Guid id = await CreatePrivateProductAsync(owner);
        string path = $"/api/v1/products/{id}";
        using HttpResponseMessage read = await stranger.GetAsync(path);
        using HttpResponseMessage update = await stranger.PatchAsJsonAsync(path, new { name = "Attacker name" });
        using HttpResponseMessage delete = await stranger.DeleteAsync(path);
        Assert.Equal(HttpStatusCode.NotFound, read.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, update.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, delete.StatusCode);
        using HttpResponseMessage unchanged = await owner.GetAsync(path);
        Assert.Equal(HttpStatusCode.OK, unchanged.StatusCode);
        JsonElement product = await unchanged.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Private cookie fixture", product.GetProperty("name").GetString());
    }

    private static async Task<Guid> CreatePrivateProductAsync(HttpClient owner) {
        using HttpResponseMessage created = await owner.PostAsJsonAsync("/api/v1/products",
            new CreateProductHttpRequest(Barcode: null, "Private cookie fixture", Brand: null, "Unknown", Category: null, Description: null, Comment: null, ImageUrl: null, ImageAssetId: null,
                "G", 100, 100, 120, 10, 5, 20, 3, 0, "Private"));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        JsonElement product = await created.Content.ReadFromJsonAsync<JsonElement>();
        return product.GetProperty("id").GetGuid();
    }
}
