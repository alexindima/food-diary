
namespace FoodDiary.Web.Api.Extensions;

public sealed class SecurityHeadersMiddleware(
    RequestDelegate next,
    IWebHostEnvironment environment) {
    private const string DefaultContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'";
    private const string SwaggerContentSecurityPolicy =
        "default-src 'none'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'; " +
        "img-src 'self' data:; font-src 'self'; connect-src 'self'; frame-ancestors 'none'; " +
        "base-uri 'none'; form-action 'none'";

    public Task InvokeAsync(HttpContext context) {
        IHeaderDictionary headers = context.Response.Headers;
        headers.XContentTypeOptions = "nosniff";
        headers.XFrameOptions = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["X-Permitted-Cross-Domain-Policies"] = "none";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        headers.ContentSecurityPolicy = ResolveContentSecurityPolicy(context.Request.Path);
        return next(context);
    }

    private string ResolveContentSecurityPolicy(PathString path) =>
        environment.IsDevelopment() && path.StartsWithSegments("/swagger", StringComparison.OrdinalIgnoreCase)
            ? SwaggerContentSecurityPolicy
            : DefaultContentSecurityPolicy;
}
