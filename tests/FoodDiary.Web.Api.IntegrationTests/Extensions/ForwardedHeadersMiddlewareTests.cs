using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class ForwardedHeadersMiddlewareTests {
    [Fact]
    public async Task Invoke_WithKnownProxy_UsesForwardedForAndProto() {
        var middleware = CreateMiddleware(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
            KnownProxies = { System.Net.IPAddress.Parse("10.0.0.10") }
        });

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("10.0.0.10");
        httpContext.Request.Scheme = "http";
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        httpContext.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(httpContext);

        Assert.Equal("203.0.113.10", httpContext.Connection.RemoteIpAddress?.ToString());
        Assert.Equal("https", httpContext.Request.Scheme);
    }

    [Fact]
    public async Task Invoke_WithUnknownProxy_IgnoresForwardedHeaders() {
        var middleware = CreateMiddleware(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
            KnownProxies = { System.Net.IPAddress.Parse("10.0.0.10") }
        });

        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.25");
        httpContext.Request.Scheme = "http";
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        httpContext.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(httpContext);

        Assert.Equal("198.51.100.25", httpContext.Connection.RemoteIpAddress?.ToString());
        Assert.Equal("http", httpContext.Request.Scheme);
    }

    [Fact]
    public void Configure_WithKnownProxiesAndNetworks_MapsTrustBoundaries() {
        var setup = new ForwardedHeadersOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiForwardedHeadersOptions {
            ForwardLimit = 2,
            KnownProxies = ["10.0.0.10"],
            KnownNetworks = ["10.0.0.0/24", "2001:db8::/32"]
        }));
        var options = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions();

        setup.Configure(options);

        Assert.Equal(2, options.ForwardLimit);
        Assert.Contains(options.KnownProxies, ip => ip.ToString() == "10.0.0.10");
        Assert.Contains(options.KnownIPNetworks, network => network.BaseAddress.ToString() == "10.0.0.0" && network.PrefixLength == 24);
        Assert.Contains(options.KnownIPNetworks, network => network.BaseAddress.ToString() == "2001:db8::" && network.PrefixLength == 32);
    }

    private static ForwardedHeadersMiddleware CreateMiddleware(Microsoft.AspNetCore.Builder.ForwardedHeadersOptions options) {
        return new ForwardedHeadersMiddleware(
            static _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Microsoft.Extensions.Options.Options.Create(options));
    }
}
