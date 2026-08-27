using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthSessionLifecycleController(ISender mediator) : BaseApiController(mediator) {
    [AllowAnonymous]
    [HttpPost("logout")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Logout() => DeleteRefreshCookieAsync();

    private Task<IActionResult> DeleteRefreshCookieAsync() {
        HttpContext.RequestServices.GetRequiredService<RefreshTokenCookieService>().Delete(HttpContext);
        return Task.FromResult<IActionResult>(NoContent());
    }
}
