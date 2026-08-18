using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Web.Api.Tests.Extensions;

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
    public async Task Middleware_ForSwaggerInDevelopment_AllowsRequiredLocalAssets() {
        (HttpContext _, IHeaderDictionary? headers) = await InvokeMiddleware("Development", "/swagger/index.html");

        string policy = headers.ContentSecurityPolicy.ToString();
        Assert.Multiple(
            () => Assert.Contains("script-src 'self' 'unsafe-inline'", policy, StringComparison.Ordinal),
            () => Assert.Contains("style-src 'self' 'unsafe-inline'", policy, StringComparison.Ordinal),
            () => Assert.Contains("connect-src 'self'", policy, StringComparison.Ordinal),
            () => Assert.Contains("frame-ancestors 'none'", policy, StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("Development", "/api/v1/users")]
    [InlineData("Production", "/swagger/index.html")]
    public async Task Middleware_OutsideDevelopmentSwagger_UsesStrictContentSecurityPolicy(
        string environmentName,
        string path) {
        (HttpContext _, IHeaderDictionary? headers) = await InvokeMiddleware(environmentName, path);

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
        IWebHostEnvironment environment = CreateEnvironment("Production");
        var middleware = new SecurityHeadersMiddleware(_ => {
            nextCalled = true;
            return Task.CompletedTask;
        }, environment);

        await middleware.InvokeAsync(new DefaultHttpContext());

        Assert.True(nextCalled);
    }

    private static async Task<(HttpContext Context, IHeaderDictionary Headers)> InvokeMiddleware(
        string environmentName = "Production",
        string path = "/api/test") {
        var context = new DefaultHttpContext();
        context.Request.Path = path;
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask, CreateEnvironment(environmentName));

        await middleware.InvokeAsync(context).ConfigureAwait(false);

        return (context, context.Response.Headers);
    }

    private static IWebHostEnvironment CreateEnvironment(string environmentName) {
        IWebHostEnvironment environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns(environmentName);
        return environment;
    }
}
