using FoodDiary.Modules.Identity.Presentation.Security;

namespace FoodDiary.Web.Api.IntegrationTests.TestInfrastructure;

[ExcludeFromCodeCoverage]
internal static class AuthenticationResponseCookies {
    public static string ReadRefreshToken(HttpResponseMessage response) {
        const string prefix = RefreshTokenCookieService.CookieName + "=";
        string cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), value => value.StartsWith(prefix, StringComparison.Ordinal));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        return Uri.UnescapeDataString(cookie[prefix.Length..].Split(';')[0]);
    }
}
