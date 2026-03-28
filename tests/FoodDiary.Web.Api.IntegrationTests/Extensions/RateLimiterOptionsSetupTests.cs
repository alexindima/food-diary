using System.Reflection;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Web.Api.IntegrationTests.Extensions;

public sealed class RateLimiterOptionsSetupTests {
    [Fact]
    public void GetPartitionKey_IgnoresSpoofedForwardedForHeader_AndUsesRemoteIp() {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.25");

        var partitionKey = InvokeGetPartitionKey(httpContext);

        Assert.Equal("ip:198.51.100.25", partitionKey);
    }

    [Fact]
    public void GetPartitionKey_NormalizesIpv6MappedIpv4Address() {
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("::ffff:198.51.100.25");

        var partitionKey = InvokeGetPartitionKey(httpContext);

        Assert.Equal("ip:198.51.100.25", partitionKey);
    }

    private static string InvokeGetPartitionKey(HttpContext httpContext) {
        var method = typeof(RateLimiterOptionsSetup).GetMethod(
            "GetPartitionKey",
            BindingFlags.NonPublic | BindingFlags.Static);

        Assert.NotNull(method);
        var result = method.Invoke(null, [httpContext]);
        var partitionKey = Assert.IsType<string>(result);
        return partitionKey;
    }
}
