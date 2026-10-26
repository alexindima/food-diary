using FoodDiary.Modules.Identity.Application.Authentication.Models;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class BrowserSessionCookieService {
    public const string Scheme = "BrowserSession";
    public const string ClientHeader = "X-FoodDiary-Client";
    public const string SessionHeader = "X-FoodDiary-Session";
    public const string ContextHeader = "X-FoodDiary-Context";
    public const string BrowserCookieName = "fooddiary.browser";
    public const string AdminCookieName = "fooddiary.admin";

    public static bool IsAdmin(HttpContext context) =>
        string.Equals(context.Request.Headers[ClientHeader], "admin", StringComparison.Ordinal) ||
        context.Request.Path.Value?.Contains("/admin/", StringComparison.OrdinalIgnoreCase) == true;

    public static string CookieName(HttpContext context) => CookieName(context, IsAdmin(context));
    public static string CookieName(HttpContext context, bool admin) =>
        (context.Request.IsHttps ? "__Host-" : string.Empty) + (admin ? AdminCookieName : BrowserCookieName);
    public string? Read(HttpContext context) => context.Request.Cookies[CookieName(context)];

    public void Set(HttpContext context, BrowserSessionModel session, bool admin = false) =>
        context.Response.Cookies.Append(CookieName(context, admin), session.Credential,
            Options(context, session.RememberMe ? new DateTimeOffset(session.ExpiresAtUtc) : null));

    public void Delete(HttpContext context) => context.Response.Cookies.Delete(CookieName(context), Options(context, expires: null));

    private static CookieOptions Options(HttpContext context, DateTimeOffset? expires) => new() {
        HttpOnly = true,
        Secure = context.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
        IsEssential = true,
        Expires = expires,
    };
}
