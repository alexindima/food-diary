using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HostFiltering;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Net;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class BrowserIngressConfigurationTests {
    [Theory]
    [InlineData("fooddiary.club", StatusCodes.Status204NoContent)]
    [InlineData("admin.fooddiary.club", StatusCodes.Status204NoContent)]
    [InlineData("attacker.example", StatusCodes.Status400BadRequest)]
    public async Task DefaultConfiguration_AcceptsApplicationHostsAndRejectsUnrelatedHosts(string host, int expectedStatus) {
        IConfiguration configuration = LoadConfiguration();
        IOptionsMonitor<HostFilteringOptions> monitor = Substitute.For<IOptionsMonitor<HostFilteringOptions>>();
        monitor.CurrentValue.Returns(new HostFilteringOptions {
            AllowedHosts = configuration["AllowedHosts"]!.Split(';'),
        });
        var middleware = new HostFilteringMiddleware(
            context => { context.Response.StatusCode = StatusCodes.Status204NoContent; return Task.CompletedTask; },
            NullLogger<HostFilteringMiddleware>.Instance, monitor);
        var context = new DefaultHttpContext();
        context.Request.Host = new HostString(host);
        context.Response.Body = new MemoryStream();

        await middleware.Invoke(context);

        Assert.Equal(expectedStatus, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("fooddiary.club")]
    [InlineData("admin.fooddiary.club")]
    public async Task DefaultConfiguration_RecognizesApplicationOriginsFromTrustedTlsIngress(string host) {
        ApiForwardedHeadersOptions settings = LoadConfiguration().GetSection("ForwardedHeaders").Get<ApiForwardedHeadersOptions>()!;
        var options = new ForwardedHeadersOptions();
        new ForwardedHeadersOptionsSetup(MsOptions.Create(settings)).Configure(options);
        var middleware = new ForwardedHeadersMiddleware(_ => Task.CompletedTask,
            NullLoggerFactory.Instance, MsOptions.Create(options));
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Loopback;
        context.Request.Host = new HostString("internal-api", 5000);
        context.Request.Scheme = "http";
        context.Request.Headers["X-Forwarded-Host"] = host;
        context.Request.Headers["X-Forwarded-Proto"] = "https";

        await middleware.Invoke(context);

        Assert.Multiple(
            () => Assert.Equal(host, context.Request.Host.Value),
            () => Assert.True(context.Request.IsHttps));
    }

    private static IConfiguration LoadConfiguration() {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "FoodDiary.slnx"))) {
            directory = directory.Parent;
        }
        Assert.NotNull(directory);
        return new ConfigurationBuilder().SetBasePath(directory.FullName)
            .AddJsonFile("FoodDiary.Web.Api/appsettings.json").Build();
    }
}
