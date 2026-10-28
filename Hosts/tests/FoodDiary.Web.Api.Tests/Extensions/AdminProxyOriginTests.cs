using System.Net;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Logging.Abstractions;
using MsOptions = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class AdminProxyOriginTests {
    [Theory]
    [InlineData(true, "https://admin.fooddiary.club", true)]
    [InlineData(false, "https://admin.fooddiary.club", false)]
    [InlineData(true, "https://fooddiary.club", false)]
    [InlineData(true, "https://attacker.example", false)]
    public async Task AdminSso_RequiresTrustedProxyAndSamePublicOrigin(bool trusted, string origin, bool allowed) {
        var options = new ForwardedHeadersOptions();
        new ForwardedHeadersOptionsSetup(MsOptions.Create(new ApiForwardedHeadersOptions {
            KnownProxies = trusted ? ["172.29.0.1"] : [],
            AllowedHosts = ["admin.fooddiary.club"],
        })).Configure(options);
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("172.29.0.1");
        context.Request.Scheme = "http";
        context.Request.Host = new HostString("admin.fooddiary.club");
        context.Request.Path = "/api/v1/auth/admin-sso/exchange";
        context.Request.Method = HttpMethods.Post;
        context.Request.Headers.XRequestedWith = "XMLHttpRequest";
        context.Request.Headers[BrowserSessionCookieService.ClientHeader] = "admin";
        context.Request.Headers.Origin = origin;
        context.Request.Headers["X-Forwarded-Proto"] = "https";
        context.Request.Headers["X-Forwarded-Host"] = "admin.fooddiary.club";
        context.Request.Headers["X-Forwarded-For"] = "203.0.113.10";
        context.Response.Body = new MemoryStream();
        IWebHostEnvironment environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        bool reachedExchange = false;
        var protection = new BrowserRequestProtectionMiddleware(_ => {
            reachedExchange = true;
            return Task.CompletedTask;
        });
        var forwarded = new ForwardedHeadersMiddleware(
            request => protection.InvokeAsync(request, MsOptions.Create(new ApiCorsOptions {
                Origins = ["https://fooddiary.club"],
            }), environment), NullLoggerFactory.Instance, MsOptions.Create(options));

        await forwarded.Invoke(context);

        Assert.Multiple(
            () => Assert.Equal(allowed, reachedExchange),
            () => Assert.Equal(trusted ? "https" : "http", context.Request.Scheme),
            () => Assert.Equal(allowed ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden, context.Response.StatusCode));
    }
}
