using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CookieAccountRevocationApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task RemovingAdminRole_RejectsExistingAdminCookieAndRestorationCannotRecoverTheRemovedPermission() {
        using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient formerAdmin = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
        CookieIdentity target = await CookieAuthenticationScenario.CreateAdministratorAsync(factory, formerAdmin);
        using HttpResponseMessage before = await formerAdmin.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, before.StatusCode);

        using HttpResponseMessage removed = await administrator.PatchAsJsonAsync($"/api/v1/admin/users/{target.Id}", new { roles = Array.Empty<string>() });
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        using HttpResponseMessage denied = await formerAdmin.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        formerAdmin.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "admin");
        using HttpResponseMessage restored = await formerAdmin.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        JsonElement session = await restored.Content.ReadFromJsonAsync<JsonElement>();
        Assert.DoesNotContain(session.GetProperty("roles").EnumerateArray(), role => string.Equals(role.GetString(), "Admin", StringComparison.Ordinal));
        using HttpResponseMessage stillDenied = await formerAdmin.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.Forbidden, stillDenied.StatusCode);
        formerAdmin.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ClientHeader);
        using HttpResponseMessage ordinaryRestore = await formerAdmin.PostAsync("/api/v1/auth/session", content: null);
        Assert.Equal(HttpStatusCode.OK, ordinaryRestore.StatusCode);
        using HttpResponseMessage newSso = await formerAdmin.PostAsync("/api/v1/auth/admin-sso/start", content: null);
        Assert.Equal(HttpStatusCode.Forbidden, newSso.StatusCode);
        await CookieAuthenticationScenario.AssertIdentityAsync(formerAdmin, target.Id);
        using HttpResponseMessage unaffected = await administrator.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, unaffected.StatusCode);
    }

    [RequiresDockerTheory]
    [InlineData("change")]
    [InlineData("reset")]
    [InlineData("administrator")]
    public async Task PasswordReplacement_RevokesBothExistingDeviceCookiesAndAllowsOnlyTheNewPassword(string operation) {
        using HttpClient computer = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient phone = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity user = await CookieAuthenticationScenario.RegisterAsync(factory, computer);
        await CookieAuthenticationScenario.LoginAsync(phone, user.Email);
        await CookieAuthenticationScenario.AssertIdentityAsync(computer, user.Id);
        await CookieAuthenticationScenario.AssertIdentityAsync(phone, user.Id);
        const string replacement = "ReplacementCookiePassword456!";
        if (string.Equals(operation, "change", StringComparison.Ordinal)) {
            using HttpResponseMessage changed = await computer.PatchAsJsonAsync("/api/v1/users/password",
                new { currentPassword = CookieAuthenticationScenario.Password, newPassword = replacement });
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        } else if (string.Equals(operation, "reset", StringComparison.Ordinal)) {
            using HttpClient recovery = CookieAuthenticationScenario.CreateBrowser(factory);
            using HttpResponseMessage requested = await recovery.PostAsJsonAsync("/api/v1/auth/password-reset/request", new { email = user.Email });
            Assert.Equal(HttpStatusCode.NoContent, requested.StatusCode);
            PasswordResetMessage reset = factory.EmailSender.GetRequiredPasswordResetMessage(user.Email);
            using HttpResponseMessage confirmed = await recovery.PostAsJsonAsync("/api/v1/auth/password-reset/confirm",
                new ConfirmPasswordResetHttpRequest(user.Id, reset.Token, replacement));
            Assert.Equal(HttpStatusCode.OK, confirmed.StatusCode);
            await CookieAuthenticationScenario.AssertIdentityAsync(recovery, user.Id);
        } else {
            using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
            await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
            using HttpResponseMessage changed = await administrator.PatchAsJsonAsync($"/api/v1/admin/users/{user.Id}/password", new { newPassword = replacement });
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        }
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(computer);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(phone);
        using HttpResponseMessage oldPassword = await phone.PostAsJsonAsync("/api/v1/auth/login",
            new LoginHttpRequest(user.Email, CookieAuthenticationScenario.Password));
        Assert.Equal(HttpStatusCode.Unauthorized, oldPassword.StatusCode);
        await CookieAuthenticationScenario.LoginAsync(phone, user.Email, replacement);
        await CookieAuthenticationScenario.AssertIdentityAsync(phone, user.Id);
    }

    [RequiresDockerFact]
    public async Task Deactivation_RejectsBothExistingCookiesRestorationAndFreshLogin() {
        using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient computer = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient phone = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
        CookieIdentity user = await CookieAuthenticationScenario.RegisterAsync(factory, computer);
        await CookieAuthenticationScenario.LoginAsync(phone, user.Email);
        using HttpResponseMessage deactivated = await administrator.PatchAsJsonAsync($"/api/v1/admin/users/{user.Id}", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(computer);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(phone);
        using HttpResponseMessage login = await phone.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(user.Email, CookieAuthenticationScenario.Password));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
        using HttpResponseMessage unaffected = await administrator.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, unaffected.StatusCode);
    }

    [RequiresDockerFact]
    public async Task AccountDeletion_RejectsBothDeviceCookiesAndRestoration() {
        using HttpClient computer = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient phone = CookieAuthenticationScenario.CreateBrowser(factory);
        CookieIdentity user = await CookieAuthenticationScenario.RegisterAsync(factory, computer);
        await CookieAuthenticationScenario.LoginAsync(phone, user.Email);
        using HttpResponseMessage deleted = await computer.DeleteAsync("/api/v1/users");
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(computer);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(phone);
    }
}
