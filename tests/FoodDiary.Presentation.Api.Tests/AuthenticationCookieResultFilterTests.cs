using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationCookieResultFilterTests {
    [Fact]
    public async Task OnResultExecutionAsync_WithAuthenticationResponse_SetsSecureHttpOnlyRefreshCookie() {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var response = new AuthenticationHttpResponse("access-token", "refresh-token", null!);
        var result = new OkObjectResult(response);
        object controller = new();
        var context = new ResultExecutingContext(actionContext, [], result, controller);
        var filter = new AuthenticationCookieResultFilter(new RefreshTokenCookieService(TimeProvider.System));

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(
            new ResultExecutedContext(actionContext, [], result, controller)));

        string setCookie = Assert.Single(httpContext.Response.Headers.SetCookie)!;
        Assert.Multiple(
            () => Assert.Contains($"{RefreshTokenCookieService.CookieName}=refresh-token", setCookie, StringComparison.Ordinal),
            () => Assert.Contains("path=/api/v1/auth", setCookie, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OnResultExecutionAsync_WithNonAuthenticationResponse_DoesNotSetCookie() {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var result = new OkObjectResult(new { Value = "unchanged" });
        object controller = new();
        var context = new ResultExecutingContext(actionContext, [], result, controller);
        var filter = new AuthenticationCookieResultFilter(new RefreshTokenCookieService(TimeProvider.System));

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(
            new ResultExecutedContext(actionContext, [], result, controller)));

        Assert.Equal(0, httpContext.Response.Headers.SetCookie.Count);
    }
}
