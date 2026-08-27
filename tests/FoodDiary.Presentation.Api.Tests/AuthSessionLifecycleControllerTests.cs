using FoodDiary.Results;
using FoodDiary.Presentation.Api.Features.Auth;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthSessionLifecycleControllerTests {
    [Fact]
    public async Task Logout_ExpiresRefreshCookie() {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<RefreshTokenCookieService>();
        var controller = new AuthSessionLifecycleController(SubstituteSender.Create(Result.Success())) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext {
                    RequestServices = services.BuildServiceProvider(),
                },
            },
        };

        IActionResult result = await controller.Logout();

        Assert.IsType<NoContentResult>(result);
        string setCookie = controller.HttpContext.Response.Headers.SetCookie.ToString();
        Assert.False(string.IsNullOrWhiteSpace(setCookie));
        Assert.Contains($"{RefreshTokenCookieService.CookieName}=", setCookie, StringComparison.Ordinal);
        Assert.Contains("httponly", setCookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("expires=", setCookie, StringComparison.OrdinalIgnoreCase);
    }
}
