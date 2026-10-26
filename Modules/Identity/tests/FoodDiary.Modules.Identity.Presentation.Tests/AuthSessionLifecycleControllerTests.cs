using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Results;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeOtherSessions;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.RevokeSession;
using FoodDiary.Modules.Identity.Application.Authentication.Commands.Logout;
using FoodDiary.Modules.Identity.Application.Authentication.Models;
using FoodDiary.Modules.Identity.Application.Authentication.Queries.GetActiveSessions;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthSessionLifecycleControllerTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetSessions_ForwardsTypedPaginationAndTheCurrentUserAndSession(bool customPage) {
        var userId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        IRequest<Result<IReadOnlyList<ActiveSessionModel>>>? sent = null;
        IReadOnlyList<ActiveSessionModel> sessions = [];
        ISender sender = SubstituteSender.Create(Result.Success(sessions), request => sent = request);
        AuthSessionLifecycleController controller = CreateController(sender);
        GetActiveSessionsHttpQuery query = customPage ? new(Page: 3, Limit: 7) : new();

        OkObjectResult response = Assert.IsType<OkObjectResult>(await controller.GetSessions(userId, sessionId, query));

        Assert.Empty(Assert.IsType<ActiveSessionHttpResponse[]>(response.Value));
        GetActiveSessionsQuery request = Assert.IsType<GetActiveSessionsQuery>(sent);
        Assert.Multiple(
            () => Assert.Equal(userId, request.UserId.Value),
            () => Assert.Equal(sessionId, request.CurrentSessionId.Value),
            () => Assert.Equal(customPage ? 3 : 1, request.Page),
            () => Assert.Equal(customPage ? 7 : 50, request.Limit));
    }

    [Fact]
    public async Task GetSessions_MapsOnlySafeSessionMetadata() {
        var sessionId = Guid.NewGuid();
        IRequest<Result<IReadOnlyList<ActiveSessionModel>>>? sentRequest = null;
        IReadOnlyList<ActiveSessionModel> sessions = [new(
            new RefreshTokenSessionId(sessionId),
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
        Assert.Equal(otherSessionId, Assert.IsType<RevokeSessionCommand>(sentRequest).SessionId.Value);
        Assert.IsType<NoContentResult>(await controller.RevokeOtherSessions(userId, currentSessionId));
        Assert.Equal(currentSessionId, Assert.IsType<RevokeOtherSessionsCommand>(sentRequest).CurrentSessionId.Value);
    }
    [Fact]
    public async Task Logout_RevokesCookieSessionAndExpiresRefreshCookie() {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(Substitute.For<IJwtTokenGenerator>());
        services.AddScoped<RefreshTokenCookieService>();
        services.AddScoped<BrowserSessionCookieService>();
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
        services.AddSingleton(Substitute.For<IJwtTokenGenerator>());
        services.AddScoped<RefreshTokenCookieService>();
        services.AddScoped<BrowserSessionCookieService>();
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
