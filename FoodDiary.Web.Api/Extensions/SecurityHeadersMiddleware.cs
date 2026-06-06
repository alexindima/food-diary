
namespace FoodDiary.Web.Api.Extensions;

public sealed class SecurityHeadersMiddleware(RequestDelegate next) {
    public Task InvokeAsync(HttpContext context) {
        IHeaderDictionary headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
        return next(context);
    }
}
