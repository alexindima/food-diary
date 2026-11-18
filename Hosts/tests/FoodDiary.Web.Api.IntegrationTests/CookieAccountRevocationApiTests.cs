using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FoodDiary.Web.Api.IntegrationTests;

[ExcludeFromCodeCoverage]
public sealed class CookieAccountRevocationApiTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory> {
    [RequiresDockerFact]
    public async Task AdminSsoCode_CannotBeExchangedAfterIssuingBrowserLogsOut() {
        using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient recipient = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
        using HttpResponseMessage start = await administrator.PostAsync("/api/v1/auth/admin-sso/start", content: null);
        Assert.Equal(HttpStatusCode.OK, start.StatusCode);
        JsonElement handoff = await start.Content.ReadFromJsonAsync<JsonElement>();

        using HttpResponseMessage logout = await administrator.PostAsync("/api/v1/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        using HttpResponseMessage exchange = await recipient.PostAsJsonAsync("/api/v1/auth/admin-sso/exchange",
            new { Code = handoff.GetProperty("code").GetString() });

        Assert.Equal(HttpStatusCode.Unauthorized, exchange.StatusCode);
        JsonElement error = await exchange.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Authentication.AdminSsoInvalidCode", error.GetProperty("error").GetString());
    }

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
        if (string.Equals(operation, "administrator", StringComparison.Ordinal)) {
            // An administrator-issued replacement is temporary until the account holder changes it.
            const string permanent = "PermanentCookiePassword111!";
            using HttpResponseMessage changed = await phone.PatchAsJsonAsync("/api/v1/users/password",
                new { currentPassword = replacement, newPassword = permanent });
            Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
            await CookieAuthenticationScenario.LoginAsync(phone, user.Email, permanent);
        }
        await CookieAuthenticationScenario.AssertIdentityAsync(phone, user.Id);
    }

    [RequiresDockerFact]
    public async Task Deactivation_RejectsBothExistingCookiesRestorationAndFreshLogin() {
        using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient registration = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient computer = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using HttpClient phone = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        using HttpClient telegram = factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = false });
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
        CookieIdentity user = await CookieAuthenticationScenario.RegisterAsync(factory, registration);
        AuthenticationResponseCookies.ApplyBrowserSession(computer, user.Cookie);
        using HttpResponseMessage phoneLogin = await phone.PostAsJsonAsync("/api/v1/auth/login",
            new LoginHttpRequest(user.Email, CookieAuthenticationScenario.Password));
        Assert.Equal(HttpStatusCode.OK, phoneLogin.StatusCode);
        AuthenticationResponseCookies.ApplyBrowserSession(phone, AuthenticationResponseCookies.ReadBrowserSession(phoneLogin));
        telegram.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "telegram");
        using HttpResponseMessage bearerLogin = await telegram.PostAsJsonAsync("/api/v1/auth/login",
            new LoginHttpRequest(user.Email, CookieAuthenticationScenario.Password));
        Assert.Equal(HttpStatusCode.OK, bearerLogin.StatusCode);
        string refreshToken = AuthenticationResponseCookies.ReadRefreshToken(bearerLogin);

        using HttpResponseMessage deactivated = await administrator.PatchAsJsonAsync($"/api/v1/admin/users/{user.Id}", new { isActive = false });
        Assert.Equal(HttpStatusCode.OK, deactivated.StatusCode);
        using HttpResponseMessage inactiveProfile = await computer.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, inactiveProfile.StatusCode);
        using HttpClient subsequent = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpResponseMessage login = await subsequent.PostAsJsonAsync("/api/v1/auth/login",
            new LoginHttpRequest(user.Email, CookieAuthenticationScenario.Password));
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);

        // Keep the original credentials unused for restoration until after the account is active again.
        using HttpResponseMessage reactivated = await administrator.PatchAsJsonAsync($"/api/v1/admin/users/{user.Id}", new { isActive = true });
        Assert.Equal(HttpStatusCode.OK, reactivated.StatusCode);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(computer);
        await CookieAuthenticationScenario.AssertRejectedSessionAsync(phone);
        using HttpResponseMessage refresh = await telegram.PostAsJsonAsync("/api/v1/auth/refresh", new { refreshToken });
        Assert.Equal(HttpStatusCode.Unauthorized, refresh.StatusCode);
        await CookieAuthenticationScenario.LoginAsync(subsequent, user.Email);
        await CookieAuthenticationScenario.AssertIdentityAsync(subsequent, user.Id);
        using HttpResponseMessage unaffected = await administrator.GetAsync("/api/v1/admin/users");
        Assert.Equal(HttpStatusCode.OK, unaffected.StatusCode);
    }

    [RequiresDockerTheory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TemporaryPassword_OnlyAllowsPasswordChangeUntilReplacement(bool bearer) {
        using HttpClient administrator = CookieAuthenticationScenario.CreateBrowser(factory);
        using HttpClient client = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.CreateAdministratorAsync(factory, administrator);
        CookieIdentity user = await CookieAuthenticationScenario.RegisterAsync(factory, client);
        const string temporary = "TemporaryCookiePassword456!";
        const string replacement = "PermanentCookiePassword789!";
        using HttpResponseMessage assigned = await administrator.PatchAsJsonAsync($"/api/v1/admin/users/{user.Id}/password", new { newPassword = temporary });
        Assert.Equal(HttpStatusCode.NoContent, assigned.StatusCode);

        if (bearer) {
            client.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "telegram");
        }
        using HttpResponseMessage login = await client.PostAsJsonAsync("/api/v1/auth/login", new LoginHttpRequest(user.Email, temporary));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        JsonElement session = await login.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(session.GetProperty("user").GetProperty("mustChangePassword").GetBoolean());
        if (bearer) {
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", session.GetProperty("accessToken").GetString());
        }

        using HttpResponseMessage denied = await client.GetAsync("/api/v1/users/info");
        Assert.Equal(HttpStatusCode.Unauthorized, denied.StatusCode);
        using HttpResponseMessage changed = await client.PatchAsJsonAsync("/api/v1/users/password", new { currentPassword = temporary, newPassword = replacement });
        Assert.Equal(HttpStatusCode.NoContent, changed.StatusCode);
        using HttpClient subsequent = CookieAuthenticationScenario.CreateBrowser(factory);
        await CookieAuthenticationScenario.LoginAsync(subsequent, user.Email, replacement);
        await CookieAuthenticationScenario.AssertIdentityAsync(subsequent, user.Id);
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
