using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth/session")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class BrowserSessionController(ISender mediator) : BaseApiController(mediator) {
    [AllowAnonymous]
    [HttpPost]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status403Forbidden)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.SessionRestoreRateLimitPolicyName)]
    public Task<IActionResult> Restore() => HandleRestoreAsync();

    private Task<IActionResult> HandleRestoreAsync() {
        if (ImpersonationCookieService.HasContext(HttpContext)) {
            if (!ImpersonationCookieService.TryReadAuthenticatedUserId(HttpContext, out Guid userId)) {
                return HandleOk(AuthHttpMappings.InvalidBrowserSessionRestoration(), static value => value.ToHttpResponse());
            }
            return HandleOk(userId.ToBrowserSessionProfileQuery(), value => value.ToImpersonationHttpResponse(HttpContext));
        }
        BrowserSessionCookieService cookies = HttpContext.RequestServices.GetRequiredService<BrowserSessionCookieService>();
        return HandleBrowserRestoreAsync(cookies);
    }

    private async Task<IActionResult> HandleBrowserRestoreAsync(BrowserSessionCookieService cookies) {
        string? credential = cookies.Read(HttpContext);
        IActionResult result = await HandleOk(credential.ToBrowserRestoreCommand(), value => {
            if (value.Renewed) {
                cookies.Set(HttpContext, value, BrowserSessionCookieService.IsAdmin(HttpContext));
            } else {
                cookies.EnsurePreviousSession(HttpContext, value.SessionId, BrowserSessionCookieService.IsAdmin(HttpContext));
            }
            return value.ToHttpResponse();
        }).ConfigureAwait(false);
        if (result is IStatusCodeActionResult { StatusCode: StatusCodes.Status401Unauthorized }) {
            bool admin = BrowserSessionCookieService.IsAdmin(HttpContext);
            string family = admin ? BrowserSessionCookieService.AdminCookieName : BrowserSessionCookieService.BrowserCookieName;
            string alternateName = (Request.IsHttps ? string.Empty : "__Host-") + family;
            HttpContext.RequestServices.GetRequiredService<ILogger<BrowserSessionController>>().LogInformation(
                new EventId(1002, "BrowserSessionRestoreTransportRejected"),
                "Browser session restoration transport rejected: cookie present {CookiePresent}, alternate cookie present {AlternateCookiePresent}, HTTPS {Https}, admin {Admin}",
                credential is not null, Request.Cookies.ContainsKey(alternateName), Request.IsHttps, admin);
        }
        return result;
    }
}
