using System.Security.Claims;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Web.Api.Extensions;
using FoodDiary.Web.Api.Options;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using CorsOptionsFactory = Microsoft.Extensions.Options.Options;

namespace FoodDiary.Web.Api.Tests.Extensions;

[ExcludeFromCodeCoverage]
public sealed class BrowserRequestProtectionMiddlewareTests {
    [Theory]
    [InlineData(null, null, false)]
    [InlineData("XMLHttpRequest", "https://evil.example", false)]
    [InlineData("XMLHttpRequest", "https://fooddiary.example", true)]
    [InlineData("XMLHttpRequest", null, true)]
    public async Task CookieMutation_RequiresHeaderAndTrustedOrigin(string? header, string? origin, bool allowed) {
        await using ServiceProvider services = new ServiceCollection().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Scheme = "https";
        context.Request.Host = new HostString("fooddiary.example");
        context.Request.Path = "/api/v1/meals";
        context.Request.Method = HttpMethods.Post;
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())], BrowserSessionCookieService.Scheme));
        if (header is not null) { context.Request.Headers.XRequestedWith = header; }
        if (origin is not null) { context.Request.Headers.Origin = origin; }
        context.Response.Body = new MemoryStream();
        bool invoked = false;
        var middleware = new BrowserRequestProtectionMiddleware(_ => { invoked = true; return Task.CompletedTask; });
        IWebHostEnvironment environment = Substitute.For<IWebHostEnvironment>();
        environment.EnvironmentName.Returns("Production");
        await middleware.InvokeAsync(context, CorsOptionsFactory.Create(new ApiCorsOptions { Origins = ["https://fooddiary.example"] }), environment);
        Assert.Equal(allowed, invoked);
        Assert.Equal(allowed ? StatusCodes.Status200OK : StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }

    [Fact]
    public async Task AnonymousLogin_AlsoRequiresCsrfHeader() {
        await using ServiceProvider services = new ServiceCollection().AddOptions().BuildServiceProvider();
        var context = new DefaultHttpContext { RequestServices = services };
        context.Request.Path = "/api/v1/auth/login";
        context.Request.Method = HttpMethods.Post;
        context.Response.Body = new MemoryStream();
        bool invoked = false;
        var middleware = new BrowserRequestProtectionMiddleware(_ => { invoked = true; return Task.CompletedTask; });
        await middleware.InvokeAsync(context, CorsOptionsFactory.Create(new ApiCorsOptions { Origins = [] }), Substitute.For<IWebHostEnvironment>());
        Assert.False(invoked);
        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
    }
}
