using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Modules.Identity.Presentation.Security;

public sealed class TelegramBrowserBindingHttpProcessor(TimeProvider timeProvider) {
    public const string CookieName = "fooddiary.telegram-browser";

    public string Read(HttpContext context) => context.Request.Cookies[CookieName] ?? string.Empty;

    public string Ensure(HttpContext context, bool renew = false) {
        string current = Read(context);
        if (current.Length == 43 && !renew) {
            return current;
        }
        string binding = current.Length == 43 ? current : SecurityTokenGenerator.GenerateUrlSafeToken();
        context.Response.Cookies.Append(CookieName, binding, new CookieOptions {
            HttpOnly = true,
            Secure = context.Request.IsHttps,
            SameSite = SameSiteMode.Lax,
            IsEssential = true,
            Path = "/api/v1/auth/telegram",
            Expires = timeProvider.GetUtcNow().AddMinutes(15),
        });
        return binding;
    }
}
