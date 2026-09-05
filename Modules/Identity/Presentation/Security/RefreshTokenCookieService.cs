using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Security;

public sealed class RefreshTokenCookieService(TimeProvider timeProvider) {
    public const string CookieName = "fooddiary.refresh";

    public string? Read(HttpContext context) => context.Request.Cookies[CookieName];

    public void Set(HttpContext context, string refreshToken) {
        context.Response.Cookies.Append(CookieName, refreshToken, CreateOptions(context, timeProvider.GetUtcNow().AddDays(30)));
    }

    public void Delete(HttpContext context) {
        context.Response.Cookies.Delete(CookieName, CreateOptions(context, DateTimeOffset.UnixEpoch));
    }

    private static CookieOptions CreateOptions(HttpContext context, DateTimeOffset expires) => new() {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        IsEssential = true,
        Path = "/api/v1/auth",
        Expires = expires,
    };
}
