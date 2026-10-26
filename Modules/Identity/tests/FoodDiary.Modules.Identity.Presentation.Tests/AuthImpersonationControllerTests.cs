using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Modules.Admin.Contracts.Commands.ExchangeAdminImpersonation;
using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Modules.Identity.Presentation.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using System.IdentityModel.Tokens.Jwt;
using CacheOptions = Microsoft.Extensions.Options.Options;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Modules.Identity.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class AuthImpersonationControllerTests {
    [Fact]
    public async Task ExchangeImpersonation_ForwardsCodeAndReturnsOnlyAnIsolatedCookieContext() {
        IRequest<Result<string>>? sent = null;
        string token = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityToken(expires: new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        ISender sender = SubstituteSender.Create(Result.Success(token), request => sent = request);
        var cache = new MemoryDistributedCache(CacheOptions.Create(new MemoryDistributedCacheOptions()));
        var cookies = new ImpersonationCookieService(new EphemeralDataProtectionProvider(), cache);
        await using ServiceProvider services = new ServiceCollection().AddSingleton(cookies).BuildServiceProvider();
        var controller = new AuthImpersonationController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { RequestServices = services } },
        };

        IActionResult result = await controller.ExchangeImpersonation(new ExchangeImpersonationHttpRequest("single-use-code"));

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        ExchangeImpersonationHttpResponse response = Assert.IsType<ExchangeImpersonationHttpResponse>(ok.Value);
        Assert.Null(response.AccessToken);
        Assert.False(string.IsNullOrWhiteSpace(response.ContextId));
        string cookie = Assert.Single(controller.Response.Headers.SetCookie)!;
        Assert.DoesNotContain(token, cookie, StringComparison.Ordinal);
        controller.Request.Headers.Cookie = cookie.Split(';')[0];
        controller.Request.Headers[BrowserSessionCookieService.ContextHeader] = response.ContextId;
        Assert.Equal(token, await cookies.ReadTokenAsync(controller.HttpContext, CancellationToken.None));
        Assert.Equal("single-use-code", Assert.IsType<ExchangeAdminImpersonationCommand>(sent).Code);
    }
}
