using FoodDiary.Modules.Identity.Presentation.Security;
using System.Net.Http.Json;
using System.Text.Json;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class AuthenticationResponseCookies {
    public static async Task<string> UpgradeAdminSessionAsync(HttpClient client) {
        client.DefaultRequestHeaders.Remove(BrowserSessionCookieService.ClientHeader);
        using HttpResponseMessage start = await client.PostAsync("/api/v1/auth/admin-sso/start", content: null).ConfigureAwait(false);
        start.EnsureSuccessStatusCode();
        JsonElement handoff = await start.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
        client.DefaultRequestHeaders.Add(BrowserSessionCookieService.ClientHeader, "admin");
        using HttpResponseMessage exchange = await client.PostAsJsonAsync("/api/v1/auth/admin-sso/exchange",
            new { Code = handoff.GetProperty("code").GetString() }).ConfigureAwait(false);
        exchange.EnsureSuccessStatusCode();
        ApplyBrowserSession(client, ReadBrowserSession(exchange));
        JsonElement session = await exchange.Content.ReadFromJsonAsync<JsonElement>().ConfigureAwait(false);
        Assert.False(session.TryGetProperty("accessToken", out _));
        return session.GetProperty("sessionId").GetString()!;
    }

    public static string ReadBrowserSession(HttpResponseMessage response) {
        string cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value =>
            value.StartsWith(BrowserSessionCookieService.BrowserCookieName + "=", StringComparison.Ordinal) ||
            value.StartsWith("__Host-" + BrowserSessionCookieService.BrowserCookieName + "=", StringComparison.Ordinal) ||
            value.StartsWith(BrowserSessionCookieService.AdminCookieName + "=", StringComparison.Ordinal) ||
            value.StartsWith("__Host-" + BrowserSessionCookieService.AdminCookieName + "=", StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        return cookie.Split(';')[0];
    }

    public static void ApplyBrowserSession(HttpClient client, string cookie) {
        client.DefaultRequestHeaders.Authorization = null;
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookie);
    }

    public static void ApplyBrowserSession(HttpRequestMessage request, string cookie) {
        request.Headers.Authorization = null;
        request.Headers.Add("Cookie", cookie);
    }

    public static string ReadRefreshToken(HttpResponseMessage response) {
        const string prefix = RefreshTokenCookieService.CookieName + "=";
        string cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(prefix, StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        return Uri.UnescapeDataString(cookie[prefix.Length..].Split(';')[0]);
    }
}
