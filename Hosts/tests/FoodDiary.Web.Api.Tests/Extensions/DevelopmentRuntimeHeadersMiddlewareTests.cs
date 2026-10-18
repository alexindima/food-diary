using System.Net;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class DevelopmentRuntimeHeadersMiddlewareTests {
    [Fact]
    public async Task RuntimeIdentity_DistinguishesInstancesOfTheSameBinarySet() {
        IWebHostEnvironment environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns("Development");
        var first = new DevelopmentRuntimeHeadersMiddleware(_ => Task.CompletedTask, environment);
        var second = new DevelopmentRuntimeHeadersMiddleware(_ => Task.CompletedTask, environment);
        DefaultHttpContext context = CreateContext();
        DefaultHttpContext repeated = CreateContext();
        DefaultHttpContext restarted = CreateContext();

        await first.InvokeAsync(context);
        await first.InvokeAsync(repeated);
        await second.InvokeAsync(restarted);

        Assert.Multiple(
            () => Assert.Equal(context.Response.Headers[DevelopmentRuntimeHeadersMiddleware.BuildHeader].ToString(), restarted.Response.Headers[DevelopmentRuntimeHeadersMiddleware.BuildHeader].ToString()),
            () => Assert.Equal(context.Response.Headers[DevelopmentRuntimeHeadersMiddleware.InstanceHeader].ToString(), repeated.Response.Headers[DevelopmentRuntimeHeadersMiddleware.InstanceHeader].ToString()),
            () => Assert.NotEqual(context.Response.Headers[DevelopmentRuntimeHeadersMiddleware.InstanceHeader].ToString(), restarted.Response.Headers[DevelopmentRuntimeHeadersMiddleware.InstanceHeader].ToString(), StringComparer.Ordinal));
    }

    private static DefaultHttpContext CreateContext() {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Path = "/api/v1/probe";
        return context;
    }
}
