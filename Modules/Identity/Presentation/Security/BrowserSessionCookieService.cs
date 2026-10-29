using System.Security.Cryptography;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Models;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class BrowserSessionCookieService(IDataProtectionProvider protection, TimeProvider timeProvider) {
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

    public void Set(HttpContext context, BrowserSessionModel session, bool admin = false) {
        context.Response.Cookies.Append(CookieName(context, admin), session.Credential,
            Options(context, session.RememberMe ? new DateTimeOffset(session.ExpiresAtUtc) : null));
        SetPreviousSession(context, session.SessionId, admin);
    }

    public void Set(HttpContext context, IssuedBrowserSession session) {
        context.Response.Cookies.Append(CookieName(context, session.IsAdmin), session.Credential,
            Options(context, session.RememberMe ? new DateTimeOffset(session.ExpiresAtUtc) : null));
        SetPreviousSession(context, session.SessionId, session.IsAdmin);
    }

    public static string PreviousCookieName(HttpContext context, bool admin) => CookieName(context, admin) + ".previous";

    public RefreshTokenSessionId? ReadPreviousSessionId(HttpContext context, bool admin) {
        string? value = context.Request.Cookies[PreviousCookieName(context, admin)];
        if (string.IsNullOrEmpty(value) || value.Length > 1024) {
            return null;
        }
        try {
            string id = ReplacementProtector(context, admin).Unprotect(value, out DateTimeOffset expires);
            return expires > timeProvider.GetUtcNow() && Guid.TryParseExact(id, "D", out Guid sessionId) && sessionId != Guid.Empty
                ? new RefreshTokenSessionId(sessionId) : null;
        } catch (CryptographicException) {
            return null;
        }
    }

    public void EnsurePreviousSession(HttpContext context, Guid sessionId, bool admin) {
        if (ReadPreviousSessionId(context, admin) != new RefreshTokenSessionId(sessionId)) {
            SetPreviousSession(context, sessionId, admin);
        }
    }

    private void SetPreviousSession(HttpContext context, Guid sessionId, bool admin) {
        DateTimeOffset expires = timeProvider.GetUtcNow().AddDays(365);
        string marker = ReplacementProtector(context, admin).Protect(sessionId.ToString("D"), expires);
        context.Response.Cookies.Append(PreviousCookieName(context, admin), marker, Options(context, expires));
    }

    private ITimeLimitedDataProtector ReplacementProtector(HttpContext context, bool admin) =>
        protection.CreateProtector("FoodDiary.BrowserSessionReplacement.v1", CookieName(context, admin)).ToTimeLimitedDataProtector();

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
