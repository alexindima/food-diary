using FoodDiary.Mediator;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Modules.Identity.Presentation.Security;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;

[ApiController]
[Authorize]
[BlockImpersonatedAccess]
[Route("api/v{version:apiVersion}/auth/telegram/unlink/oidc")]
[RequestSizeLimit(AuthRequestLimits.MaxPayloadBytes)]
[RejectOversizedRequest(AuthRequestLimits.MaxPayloadBytes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
[ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
[ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
[ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
[ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
[ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
public sealed class AuthTelegramUnlinkController(ISender mediator, TelegramBrowserBindingHttpProcessor bindings) : BaseApiController(mediator) {
    [HttpPost("start")]
    [ProducesResponseType<TelegramOidcStartHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> StartUnlink([FromCurrentUser] Guid userId) =>
        HandleOk(TelegramOnboardingHttpMappings.ToUnlinkStartCommand(userId, bindings.Ensure(HttpContext)), static model => model.ToHttpResponse());

    [HttpPost("complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> CompleteUnlink([FromCurrentUser] Guid userId, [FromBody] ExchangeTelegramOidcHttpRequest request) =>
        HandleNoContent(request.ToUnlinkCompleteCommand(userId, bindings.Read(HttpContext)));
}
