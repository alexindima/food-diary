using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Web.Api.Options;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Extensions;

public sealed class BrowserRequestProtectionMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context, IOptions<ApiCorsOptions> cors, IWebHostEnvironment environment) {
        bool bearer = context.Request.Headers.Authorization.ToString().StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ||
            (context.Request.Path.StartsWithSegments("/hubs", StringComparison.OrdinalIgnoreCase) && context.Request.Query.ContainsKey("access_token"));
        string path = context.Request.Path.Value ?? string.Empty;
        bool authEntry = path.Contains("/auth/", StringComparison.OrdinalIgnoreCase) &&
            !path.Contains("/telegram/bot/", StringComparison.OrdinalIgnoreCase);
        bool browser = context.User.Identity?.IsAuthenticated == true &&
            (string.Equals(context.User.Identity.AuthenticationType, BrowserSessionCookieService.Scheme, StringComparison.Ordinal) || ImpersonationCookieService.HasContext(context));
        bool unsafeMethod = !HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method) && !HttpMethods.IsOptions(context.Request.Method);
        if (!bearer && (browser || authEntry) && unsafeMethod &&
            context.Request.Headers.XRequestedWith != "XMLHttpRequest") {
            await RejectAsync(context, context.RequestAborted).ConfigureAwait(false);
            return;
        }
        string origin = context.Request.Headers.Origin.ToString();
        string expectedSession = context.Request.Headers[BrowserSessionCookieService.SessionHeader].ToString();
        if (!bearer && expectedSession.Length > 0 &&
            BrowserSessionCredential.TryReadSessionId(context.Request.Cookies[BrowserSessionCookieService.CookieName(context)], out Guid actualSession) &&
            !string.Equals(expectedSession, actualSession.ToString("D"), StringComparison.Ordinal)) {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new ApiErrorHttpResponse(
                "Authentication.SessionChanged", "The request belongs to another session.", context.TraceIdentifier), context.RequestAborted).ConfigureAwait(false);
            return;
        }
        string ownOrigin = $"{context.Request.Scheme}://{context.Request.Host}";
        if (!bearer && BrowserSessionCookieService.IsAdmin(context) && !environment.IsDevelopment() && origin.Length > 0 &&
            !string.Equals(origin, ownOrigin, StringComparison.OrdinalIgnoreCase)) {
            await RejectAsync(context, context.RequestAborted).ConfigureAwait(false);
            return;
        }
        if (!bearer && (browser || authEntry) && origin.Length > 0 &&
            !string.Equals(origin, ownOrigin, StringComparison.OrdinalIgnoreCase) &&
            !cors.Value.Origins.Contains(origin, StringComparer.OrdinalIgnoreCase)) {
            await RejectAsync(context, context.RequestAborted).ConfigureAwait(false);
            return;
        }
        await next(context).ConfigureAwait(false);
    }

    private static Task RejectAsync(HttpContext context, CancellationToken cancellationToken) {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return context.Response.WriteAsJsonAsync(new ApiErrorHttpResponse(
            "Authentication.InvalidBrowserRequest", "The browser request could not be verified.", context.TraceIdentifier), cancellationToken);
    }
}
