using FoodDiary.Authentication.Contracts.Authentication.Common;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Application.Abstractions.Authentication.Abstractions;
using FoodDiary.Modules.Identity.Contracts.Authentication.Common;
using FoodDiary.Modules.Identity.Domain.ValueObjects.Ids;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class LogoutRevocationFailureTests {
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Logout_OnlyExpiresTheCookieAfterConfirmedRevocation(bool telegram, bool throws) {
        var id = Guid.NewGuid();
        IJwtTokenGenerator tokens = Substitute.For<IJwtTokenGenerator>();
        tokens.ValidateToken("refresh-proof").Returns((UserId.New(), "user@example.com", false, new RefreshTokenSessionId(id)));
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<IRequest<Result>>(), Arg.Any<CancellationToken>()).Returns(throws
            ? Task.FromException<Result>(new InvalidOperationException("revocation failed"))
            : Task.FromResult(Result.Failure(AuthenticationErrors.InvalidToken)));
        await using ServiceProvider provider = new ServiceCollection().AddSingleton(tokens)
            .AddDataProtection().Services.AddSingleton(TimeProvider.System)
            .AddScoped<BrowserSessionCookieService>().AddScoped<RefreshTokenCookieService>().BuildServiceProvider();
        var controller = new AuthSessionLifecycleController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = provider } },
        };
        controller.Request.Headers.Cookie = telegram
            ? RefreshTokenCookieService.CookieName + "=refresh-proof"
            : BrowserSessionCookieService.BrowserCookieName + "=" + BrowserSessionCredential.Create(id);
        controller.Request.Headers[BrowserSessionCookieService.ClientHeader] = telegram ? "telegram" : "web";
        controller.Request.Headers[BrowserSessionCookieService.SessionHeader] = id.ToString("D");
        if (throws) {
            await Assert.ThrowsAsync<InvalidOperationException>(() => controller.Logout());
        } else {
            ObjectResult failed = Assert.IsAssignableFrom<ObjectResult>(await controller.Logout());
            Assert.Equal(StatusCodes.Status401Unauthorized, failed.StatusCode);
        }
        Assert.True(controller.Response.Headers.SetCookie.Count == 0,
            "Failed server revocation must keep the credential available for a scoped logout retry.");
    }
}
