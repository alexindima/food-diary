using System.Text.Json;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Identity.Presentation.Filters;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Routing;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using System.IdentityModel.Tokens.Jwt;
using FoodDiary.Authentication.Contracts.Authentication.Common;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthenticationCookieResultFilterTests {
    [Fact]
    public async Task FailedBrowserSessionCreation_ReturnsUnauthorizedWithoutLeakingInternalTokensOrSettingCookies() {
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/v1/auth/login";
        var action = new ActionContext(http, new RouteData(), new ActionDescriptor());
        var response = new OkObjectResult(new AuthenticationHttpResponse("internal-access", "internal-refresh", null!));
        var context = new ResultExecutingContext(action, [], response, new object());
        var filter = new AuthenticationCookieResultFilter(new RefreshTokenCookieService(CreateTokenGenerator()),
            new BrowserSessionCookieService(), SubstituteSender.Create(Result.Failure<BrowserSessionModel>(AuthenticationErrors.InvalidToken)));
        bool continued = false;
        await filter.OnResultExecutionAsync(context, () => {
            continued = true;
            Assert.IsType<UnauthorizedResult>(context.Result);
            return Task.FromResult(new ResultExecutedContext(action, [], context.Result, context.Controller));
        });
        Assert.True(continued);
        Assert.Equal(0, http.Response.Headers.SetCookie.Count);
        Assert.IsType<UnauthorizedResult>(context.Result);
    }

    [Fact]
    public async Task OnResultExecutionAsync_WithAdminSsoActionPolicy_PreservesExistingMainRefreshSession() {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Headers.Cookie = $"{RefreshTokenCookieService.CookieName}=main-session";
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        string token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(expires: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        var response = new AuthenticationHttpResponse(token, "admin-refresh-token", null!);
        var result = new OkObjectResult(response);
        object controller = new();
        // MVC composes IFilterMetadata attributes from the actual action into this list.
        var filters = typeof(AdminSsoController).GetMethod(nameof(AdminSsoController.AdminSsoExchange))!
            .GetCustomAttributes(inherit: true).OfType<IFilterMetadata>().ToList();
        Assert.Single(filters.OfType<SuppressRefreshTokenCookieAttribute>());
        var context = new ResultExecutingContext(actionContext, filters, result, controller);
        httpContext.Request.Path = "/api/v1/auth/admin-sso/exchange";
        AuthenticationCookieResultFilter filter = CreateFilter();
        bool continued = false;

        await filter.OnResultExecutionAsync(context, () => {
            continued = true;
            Assert.Same(result, context.Result);
            return Task.FromResult(new ResultExecutedContext(actionContext, filters, result, controller));
        });

        Assert.True(continued);
        Assert.Contains("fooddiary.admin=", Assert.Single(httpContext.Response.Headers.SetCookie)!, StringComparison.Ordinal);
        Assert.Equal("main-session", new RefreshTokenCookieService(CreateTokenGenerator()).Read(httpContext));
        Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        Assert.Null(Assert.IsType<AuthenticationHttpResponse>(result.Value).AccessToken);
        string json = JsonSerializer.Serialize(result.Value, JsonSerializerOptions.Web);
        Assert.DoesNotContain("accessToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("admin-refresh-token", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OnResultExecutionAsync_WithAuthenticationResponse_SetsSecureHttpOnlyRefreshCookie() {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var response = new AuthenticationHttpResponse("access-token", "refresh-token", null!);
        var result = new OkObjectResult(response);
        object controller = new();
        var context = new ResultExecutingContext(actionContext, [], result, controller);
        httpContext.Request.Path = "/api/v1/auth/telegram/complete";
        httpContext.Request.Headers[BrowserSessionCookieService.ClientHeader] = "telegram";
        AuthenticationCookieResultFilter filter = CreateFilter();

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(
            new ResultExecutedContext(actionContext, [], result, controller)));

        string setCookie = Assert.Single(httpContext.Response.Headers.SetCookie)!;
        string json = JsonSerializer.Serialize(response, JsonSerializerOptions.Web);
        Assert.DoesNotContain("refreshToken", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh-token", json, StringComparison.Ordinal);
        Assert.Contains("access-token", json, StringComparison.Ordinal);
        Assert.Multiple(
            () => Assert.Contains($"{RefreshTokenCookieService.CookieName}=refresh-token", setCookie, StringComparison.Ordinal),
            () => Assert.Contains("path=/api/v1/auth", setCookie, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains("secure", setCookie, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains("samesite=lax", setCookie, StringComparison.OrdinalIgnoreCase),
            () => Assert.Contains("expires=Fri, 28 Jun 2030 12:00:00 GMT", setCookie, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task OnResultExecutionAsync_WithNonAuthenticationResponse_DoesNotSetCookie() {
        var httpContext = new DefaultHttpContext();
        var actionContext = new ActionContext(httpContext, new RouteData(), new ActionDescriptor());
        var result = new OkObjectResult(new { Value = "unchanged" });
        object controller = new();
        var context = new ResultExecutingContext(actionContext, [], result, controller);
        AuthenticationCookieResultFilter filter = CreateFilter();

        await filter.OnResultExecutionAsync(context, () => Task.FromResult(
            new ResultExecutedContext(actionContext, [], result, controller)));

        Assert.Equal(0, httpContext.Response.Headers.SetCookie.Count);
    }

    private static IJwtTokenGenerator CreateTokenGenerator() {
        IJwtTokenGenerator generator = Substitute.For<IJwtTokenGenerator>();
        generator.ReadRefreshTokenExpirationUtc(Arg.Any<string>())
            .Returns(new DateTime(2030, 6, 28, 12, 0, 0, DateTimeKind.Utc));
        return generator;
    }

    private static AuthenticationCookieResultFilter CreateFilter() {
        var id = Guid.NewGuid();
        var principal = new UserAuthenticationPrincipalModel(UserId.New(), "test@example.com", ["Admin"], AccessTokenCapUtc: null, User: null!);
        var session = new BrowserSessionModel(BrowserSessionCredential.Create(id), id,
            new DateTime(2030, 6, 28, 12, 0, 0, DateTimeKind.Utc), RememberMe: true, principal);
        return new AuthenticationCookieResultFilter(new RefreshTokenCookieService(CreateTokenGenerator()),
            new BrowserSessionCookieService(), SubstituteSender.Create(Result.Success(session)));
    }
}
