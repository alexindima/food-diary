using FoodDiary.Results;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Application.Identity.Authentication.Commands.RevokeOtherSessions;
using FoodDiary.Application.Identity.Authentication.Commands.RevokeSession;
using FoodDiary.Application.Identity.Authentication.Commands.Logout;
using FoodDiary.Application.Identity.Authentication.Models;
using FoodDiary.Application.Identity.Authentication.Queries.GetActiveSessions;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthSessionLifecycleControllerTests {
    [Fact]
    public async Task GetSessions_MapsOnlySafeSessionMetadata() {
        var sessionId = Guid.NewGuid();
        IRequest<Result<IReadOnlyList<ActiveSessionModel>>>? sentRequest = null;
        IReadOnlyList<ActiveSessionModel> sessions = [new(
            sessionId,
            IsCurrent: true,
            "password",
            "Chrome",
            "Windows",
            "Desktop",
            CreatedAtUtc: DateTime.UtcNow,
            LastActiveAtUtc: DateTime.UtcNow)];
        ISender sender = SubstituteSender.Create(Result.Success(sessions), request => sentRequest = request);
        AuthSessionLifecycleController controller = CreateController(sender);

        IActionResult result = await controller.GetSessions(Guid.NewGuid(), sessionId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ActiveSessionHttpResponse response = Assert.Single(Assert.IsType<ActiveSessionHttpResponse[]>(ok.Value));
        Assert.True(response.IsCurrent);
        Assert.IsType<GetActiveSessionsQuery>(sentRequest);
    }

    [Fact]
    public async Task RevokeEndpoints_SendUserScopedIdempotentCommands() {
        IRequest<Result>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(), request => sentRequest = request);
        AuthSessionLifecycleController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var currentSessionId = Guid.NewGuid();
        var otherSessionId = Guid.NewGuid();

        Assert.IsType<NoContentResult>(await controller.RevokeSession(userId, currentSessionId, otherSessionId));
        Assert.Equal(otherSessionId, Assert.IsType<RevokeSessionCommand>(sentRequest).SessionId);
        Assert.IsType<NoContentResult>(await controller.RevokeOtherSessions(userId, currentSessionId));
        Assert.Equal(currentSessionId, Assert.IsType<RevokeOtherSessionsCommand>(sentRequest).CurrentSessionId);
    }
    [Fact]
    public async Task Logout_RevokesCookieSessionAndExpiresRefreshCookie() {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<RefreshTokenCookieService>();
        IRequest<Result>? sentRequest = null;
        var controller = new AuthSessionLifecycleController(
            SubstituteSender.Create(Result.Success(), request => sentRequest = request)) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    RequestServices = services.BuildServiceProvider(),
                },
            },
        };
        controller.HttpContext.Request.Headers.Cookie = $"{RefreshTokenCookieService.CookieName}=cookie-refresh-token";

        IActionResult result = await controller.Logout();

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("cookie-refresh-token", Assert.IsType<LogoutCommand>(sentRequest).RefreshToken);
        string setCookie = controller.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.False(string.IsNullOrWhiteSpace(setCookie));
        Assert.Contains($"{RefreshTokenCookieService.CookieName}=", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Logout_RequestTokenSupportsLegacyClientAndTakesPrecedenceOverCookie() {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<RefreshTokenCookieService>();
        IRequest<Result>? sentRequest = null;
        var controller = new AuthSessionLifecycleController(
            SubstituteSender.Create(Result.Success(), request => sentRequest = request)) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    RequestServices = services.BuildServiceProvider(),
                },
            },
        };
        controller.HttpContext.Request.Headers.Cookie = $"{RefreshTokenCookieService.CookieName}=cookie-refresh-token";

        IActionResult result = await controller.Logout(new RefreshTokenHttpRequest("legacy-refresh-token"));

        Assert.IsType<NoContentResult>(result);
        Assert.Equal("legacy-refresh-token", Assert.IsType<LogoutCommand>(sentRequest).RefreshToken);
    }

    private static AuthSessionLifecycleController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
