using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class SecurityHeadersMiddlewareTests {
    [Fact]
    public async Task Middleware_SetsXContentTypeOptions() {
        (HttpContext _, IHeaderDictionary? headers) = await InvokeMiddleware();

        Assert.Equal("nosniff", headers.XContentTypeOptions.ToString());
    }

    [Fact]
    public async Task Middleware_SetsXFrameOptions() {
        (HttpContext _, IHeaderDictionary? headers) = await InvokeMiddleware();

        Assert.Equal("DENY", headers.XFrameOptions.ToString());
    }

    [Fact]
    public async Task Middleware_SetsReferrerPolicy() {
        (HttpContext _, IHeaderDictionary? headers) = await InvokeMiddleware();

        Assert.Equal("strict-origin-when-cross-origin", headers["Referrer-Policy"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsPermissionsPolicy() {
        (HttpContext _, IHeaderDictionary? headers) = await InvokeMiddleware();

        Assert.Equal("camera=(), microphone=(), geolocation=()", headers["Permissions-Policy"].ToString());
    }

    [Fact]
    public async Task Middleware_SetsContentSecurityPolicy() {
        (HttpContext _, IHeaderDictionary? headers) = await InvokeMiddleware();

        Assert.Equal("default-src 'none'; frame-ancestors 'none'", headers.ContentSecurityPolicy.ToString());
    }

    [Fact]
    public async Task Middleware_SetsXPermittedCrossDomainPolicies() {
        (HttpContext _, IHeaderDictionary? headers) = await InvokeMiddleware();

        Assert.Equal("none", headers["X-Permitted-Cross-Domain-Policies"].ToString());
    }

    [Fact]
    public async Task Middleware_CallsNextDelegate() {
        bool nextCalled = false;
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

        await middleware.InvokeAsync(context).ConfigureAwait(false);

        return (context, context.Response.Headers);
    }
}
