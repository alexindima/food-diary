using System.Net;
using FoodDiary.Web.Api.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
[Collection(RuntimeDiagnosticsCollection.Name)]
public sealed class DevelopmentRuntimeHeadersMiddlewareTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task InvalidDiagnosticBinary_DoesNotBreakThePipelineOrPublishStaleHeaders(bool lockFile) {
        string fixturePath = Path.Combine(AppContext.BaseDirectory, "FoodDiary.runtime-diag-fixture-" + Guid.NewGuid().ToString("N") + ".dll");
        FileStream? heldFile = null;
        try {
            await File.WriteAllBytesAsync(fixturePath, [1, 2, 3, 4]);
            if (lockFile) {
                heldFile = File.Open(fixturePath, FileMode.Open, FileAccess.Read, FileShare.None);
            }
            IWebHostEnvironment environment = Substitute.For<IWebHostEnvironment>();
            environment.EnvironmentName.Returns("Development");
            bool invoked = false;
            var middleware = new DevelopmentRuntimeHeadersMiddleware(context => {
                invoked = true;
                context.Response.StatusCode = StatusCodes.Status204NoContent;
                return Task.CompletedTask;
            }, environment);
            DefaultHttpContext context = CreateContext();
            context.Response.Headers[DevelopmentRuntimeHeadersMiddleware.BuildHeader] = "stale-build";
            context.Response.Headers[DevelopmentRuntimeHeadersMiddleware.InstanceHeader] = "stale-instance";

            await middleware.InvokeAsync(context);

            Assert.Multiple(
                () => Assert.True(invoked),
                () => Assert.Equal(StatusCodes.Status204NoContent, context.Response.StatusCode),
                () => Assert.False(context.Response.Headers.ContainsKey(DevelopmentRuntimeHeadersMiddleware.BuildHeader)),
                () => Assert.False(context.Response.Headers.ContainsKey(DevelopmentRuntimeHeadersMiddleware.InstanceHeader)));
        } finally {
            if (heldFile is not null) { await heldFile.DisposeAsync(); }
            File.Delete(fixturePath);
        }
    }

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
