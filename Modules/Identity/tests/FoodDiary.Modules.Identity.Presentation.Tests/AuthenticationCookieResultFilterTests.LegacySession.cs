using System.Security.Claims;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Identity.Presentation.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

public sealed partial class AuthenticationCookieResultFilterTests {
    [Fact]
    public async Task BrowserConversion_ExpiresTheLegacyRefreshCookieAndReturnsOnlySessionMetadata() {
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/v1/auth/login";
        http.Request.Headers.Cookie = $"{RefreshTokenCookieService.CookieName}=synthetic-legacy-refresh";
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var response = new OkObjectResult(new AuthenticationHttpResponse("internal-access", "internal-refresh", null!));
        var context = new ResultExecutingContext(action, [], response, new object());

        await CreateFilter().OnResultExecutionAsync(context, () => Task.FromResult(
            new ResultExecutedContext(action, [], context.Result, context.Controller)));

        AuthenticationHttpResponse body = Assert.IsType<AuthenticationHttpResponse>(response.Value);
        Assert.Multiple(
            () => Assert.Null(body.AccessToken),
            () => Assert.Empty(body.RefreshToken),
            () => Assert.Equal(new[] { "Admin" }, body.Roles),
            () => Assert.NotNull(body.SessionId));
        Assert.Contains(http.Response.Headers.SetCookie, value => value!.StartsWith(
            RefreshTokenCookieService.CookieName + "=;", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AdministrativeConversion_RejectsAMissingInternalTicketBeforeIssuingCookies() {
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/v1/auth/admin-sso/exchange";
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var response = new OkObjectResult(new AuthenticationHttpResponse(AccessToken: null, "internal-refresh", null!));
        var context = new ResultExecutingContext(action, [], response, new object());
        bool continued = false;

        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateFilter().OnResultExecutionAsync(context, () => {
            continued = true;
            return Task.FromResult(new ResultExecutedContext(action, [], context.Result, context.Controller));
        }));

        Assert.False(continued);
        Assert.Equal(0, http.Response.Headers.SetCookie.Count);
    }

    [Fact]
    public void ImpersonatedRoles_IncludeEveryRoleAndExcludeUnrelatedClaims() {
        var context = new DefaultHttpContext {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Role, "Support"), new Claim(ClaimTypes.Email, "owned@example.test"),
                new Claim(ClaimTypes.Role, "Premium"),
            ], "owned-test")),
        };

        Assert.Equal(new[] { "Support", "Premium" }, ImpersonationCookieService.ReadRoles(context));
    }
}
