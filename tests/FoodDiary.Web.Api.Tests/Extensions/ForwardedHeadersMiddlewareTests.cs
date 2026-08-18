using System.Net;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class ForwardedHeadersMiddlewareTests {
    [Fact]
    public async Task Invoke_WithKnownProxy_UsesForwardedForAndProto() {
        ForwardedHeadersMiddleware middleware = CreateMiddleware(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
            KnownProxies = { System.Net.IPAddress.Parse("10.0.0.10") },
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
        ForwardedHeadersMiddleware middleware = CreateMiddleware(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
            ForwardLimit = 1,
            KnownProxies = { System.Net.IPAddress.Parse("10.0.0.10") },
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
    public async Task Invoke_WithKnownProxyAndAllowedForwardedHost_UsesForwardedHost() {
        ForwardedHeadersMiddleware middleware = CreateMiddleware(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedHost,
            ForwardLimit = 1,
            KnownProxies = { IPAddress.Parse("10.0.0.10") },
            AllowedHosts = { "fooddiary.club" },
        });
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.10");
        httpContext.Request.Host = new HostString("internal-api", 5000);
        httpContext.Request.Headers["X-Forwarded-Host"] = "fooddiary.club";

        await middleware.Invoke(httpContext);

        Assert.Equal("fooddiary.club", httpContext.Request.Host.Value);
    }

    [Fact]
    public async Task Invoke_WithKnownProxyAndDisallowedForwardedHost_IgnoresForwardedHost() {
        ForwardedHeadersMiddleware middleware = CreateMiddleware(new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions {
            ForwardedHeaders = ForwardedHeaders.XForwardedHost,
            ForwardLimit = 1,
            KnownProxies = { IPAddress.Parse("10.0.0.10") },
            AllowedHosts = { "fooddiary.club" },
        });
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("10.0.0.10");
        httpContext.Request.Host = new HostString("internal-api", 5000);
        httpContext.Request.Headers["X-Forwarded-Host"] = "attacker.example";

        await middleware.Invoke(httpContext);

        Assert.Equal("internal-api:5000", httpContext.Request.Host.Value);
    }

    [Fact]
    public void Configure_WithKnownProxiesAndNetworks_MapsTrustBoundaries() {
        var setup = new ForwardedHeadersOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiForwardedHeadersOptions {
            ForwardLimit = 2,
            KnownProxies = ["10.0.0.10"],
            KnownNetworks = ["10.0.0.0/24", "2001:db8::/32"],
            AllowedHosts = ["fooddiary.club"],
        }));
        var options = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions();

        setup.Configure(options);

        Assert.Equal(2, options.ForwardLimit);
        Assert.Equal(["fooddiary.club"], options.AllowedHosts);
        Assert.Contains(options.KnownProxies, ip => string.Equals(ip.ToString(), "10.0.0.10", StringComparison.Ordinal));
        Assert.Contains(options.KnownIPNetworks, network => string.Equals(network.BaseAddress.ToString(), "10.0.0.0", StringComparison.Ordinal) && network.PrefixLength == 24);
        Assert.Contains(options.KnownIPNetworks, network => string.Equals(network.BaseAddress.ToString(), "2001:db8::", StringComparison.Ordinal) && network.PrefixLength == 32);
    }

    [Fact]
    public void Configure_WithoutExplicitTrustBoundary_PreservesFrameworkLoopbackDefaults() {
        var setup = new ForwardedHeadersOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiForwardedHeadersOptions()));
        var options = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions();
        IPAddress[] defaultProxies = [.. options.KnownProxies];
        System.Net.IPNetwork[] defaultNetworks = [.. options.KnownIPNetworks];

        setup.Configure(options);

        Assert.Multiple(
            () => Assert.Equal(defaultProxies, options.KnownProxies),
            () => Assert.Equal(defaultNetworks, options.KnownIPNetworks));
    }

    [Fact]
    public async Task Invoke_WithDefaultSetupAndLoopbackProxy_UsesForwardedHeaders() {
        Microsoft.AspNetCore.Builder.ForwardedHeadersOptions options = CreateDefaultConfiguredOptions();
        ForwardedHeadersMiddleware middleware = CreateMiddleware(options);
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        httpContext.Request.Scheme = "http";
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        httpContext.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(httpContext);

        Assert.Multiple(
            () => Assert.Equal("203.0.113.10", httpContext.Connection.RemoteIpAddress?.ToString()),
            () => Assert.Equal("https", httpContext.Request.Scheme));
    }

    [Fact]
    public async Task Invoke_WithDefaultSetupAndUnknownProxy_IgnoresForwardedHeaders() {
        Microsoft.AspNetCore.Builder.ForwardedHeadersOptions options = CreateDefaultConfiguredOptions();
        ForwardedHeadersMiddleware middleware = CreateMiddleware(options);
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("198.51.100.25");
        httpContext.Request.Scheme = "http";
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        httpContext.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(httpContext);

        Assert.Multiple(
            () => Assert.Equal("198.51.100.25", httpContext.Connection.RemoteIpAddress?.ToString()),
            () => Assert.Equal("http", httpContext.Request.Scheme));
    }

    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(-1, false)]
    public void HasValidForwardLimit_ReturnsExpectedResult(int forwardLimit, bool expected) {
        var options = new ApiForwardedHeadersOptions { ForwardLimit = forwardLimit };

        bool valid = ApiForwardedHeadersOptions.HasValidForwardLimit(options);

        Assert.Equal(expected, valid);
    }

    [Theory]
    [InlineData("10.0.0.10", true)]
    [InlineData("2001:db8::1", true)]
    [InlineData("not-an-ip", false)]
    public void HasValidKnownProxies_ReturnsExpectedResult(string proxy, bool expected) {
        var options = new ApiForwardedHeadersOptions { KnownProxies = [proxy] };

        bool valid = ApiForwardedHeadersOptions.HasValidKnownProxies(options);

        Assert.Equal(expected, valid);
    }

    [Theory]
    [InlineData("10.0.0.0/24", true)]
    [InlineData("2001:db8::/32", true)]
    [InlineData("", false)]
    [InlineData("10.0.0.0", false)]
    [InlineData("not-an-ip/24", false)]
    [InlineData("10.0.0.0/not-a-prefix", false)]
    [InlineData("10.0.0.0/-1", false)]
    [InlineData("10.0.0.0/33", false)]
    [InlineData("2001:db8::/129", false)]
    public void HasValidKnownNetworks_ReturnsExpectedResult(string network, bool expected) {
        var options = new ApiForwardedHeadersOptions { KnownNetworks = [network] };

        bool valid = ApiForwardedHeadersOptions.HasValidKnownNetworks(options);

        Assert.Equal(expected, valid);
    }

    private static ForwardedHeadersMiddleware CreateMiddleware(Microsoft.AspNetCore.Builder.ForwardedHeadersOptions options) {
        return new ForwardedHeadersMiddleware(
            static _ => Task.CompletedTask,
            NullLoggerFactory.Instance,
            Microsoft.Extensions.Options.Options.Create(options));
    }

    private static Microsoft.AspNetCore.Builder.ForwardedHeadersOptions CreateDefaultConfiguredOptions() {
        var options = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions();
        new ForwardedHeadersOptionsSetup(Microsoft.Extensions.Options.Options.Create(new ApiForwardedHeadersOptions()))
            .Configure(options);
        return options;
    }
}
