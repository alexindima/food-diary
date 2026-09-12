using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth/telegram")]
[RequestSizeLimit(AuthRequestLimits.MaxPayloadBytes)]
[RejectOversizedRequest(AuthRequestLimits.MaxPayloadBytes)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
[EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
[ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
[ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
[ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
[ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
[ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
public sealed class AuthTelegramBackupEmailController(ISender mediator, TelegramBrowserBindingHttpProcessor bindings) : BaseApiController(mediator) {
    [Authorize]
    [BlockImpersonatedAccess]
    [HttpPost("backup-email/oidc/start")]
    [ProducesResponseType<TelegramOidcStartHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> StartBackupEmail([FromCurrentUser] Guid userId, [FromBody] StartTelegramBackupEmailHttpRequest request) =>
        HandleOk(request.ToBackupEmailStartCommand(userId, bindings.Ensure(HttpContext)), static model => model.ToHttpResponse());

    [Authorize]
    [BlockImpersonatedAccess]
    [HttpPost("backup-email/oidc/complete")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> CompleteBackupEmail([FromCurrentUser] Guid userId, [FromBody] ExchangeTelegramOidcHttpRequest request) =>
        HandleNoContent(request.ToBackupEmailCompleteCommand(userId, bindings.Read(HttpContext)));

}
