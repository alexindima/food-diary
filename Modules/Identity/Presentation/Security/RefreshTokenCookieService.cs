using Microsoft.AspNetCore.Http;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class RefreshTokenCookieService(IJwtTokenGenerator jwtTokenGenerator) {
    public const string CookieName = "fooddiary.refresh";

    public string? Read(HttpContext context) => context.Request.Cookies[CookieName];

    public void Set(HttpContext context, string refreshToken) {
        DateTime expiresAtUtc = jwtTokenGenerator.ReadRefreshTokenExpirationUtc(refreshToken);
        context.Response.Cookies.Append(CookieName, refreshToken, CreateOptions(context, new DateTimeOffset(expiresAtUtc)));
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
