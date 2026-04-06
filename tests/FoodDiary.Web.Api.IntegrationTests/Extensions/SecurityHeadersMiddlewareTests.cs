using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class SecurityHeadersMiddlewareTests {
    [Fact]
    public async Task Middleware_SetsXContentTypeOptions() {
        var (_, headers) = await InvokeMiddleware();

        Assert.Equal("nosniff", headers["X-Content-Type-Options"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsXFrameOptions() {
        var (_, headers) = await InvokeMiddleware();

        Assert.Equal("DENY", headers["X-Frame-Options"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsReferrerPolicy() {
        var (_, headers) = await InvokeMiddleware();

        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsPermissionsPolicy() {
        var (_, headers) = await InvokeMiddleware();

        Assert.Equal("camera=(), microphone=(), geolocation=()", headers["Permissions-Policy"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsContentSecurityPolicy() {
        var (_, headers) = await InvokeMiddleware();

        Assert.Equal("default-src 'none'; frame-ancestors 'none'", headers["Content-Security-Policy"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsXPermittedCrossDomainPolicies() {
        var (_, headers) = await InvokeMiddleware();

        Assert.Equal("none", headers["X-Permitted-Cross-Domain-Policies"].ToString());
    }

    [Fact]
    public async Task Middleware_CallsNextDelegate() {
        var nextCalled = false;
        var middleware = new SecurityHeadersMiddleware(_ => {
            nextCalled = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(new DefaultHttpContext());

        Assert.True(nextCalled);
    }

    private static async Task<(HttpContext Context, IHeaderDictionary Headers)> InvokeMiddleware() {
        var context = new DefaultHttpContext();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        return (context, context.Response.Headers);
    }
}
