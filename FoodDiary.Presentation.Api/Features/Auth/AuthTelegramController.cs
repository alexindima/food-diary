using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth/telegram")]
public sealed class AuthTelegramController(ISender mediator) : BaseApiController(mediator) {
    [HttpPost("verify")]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> TelegramVerify([FromBody] TelegramAuthHttpRequest request) =>
        HandleOk(request.ToCommand(HttpContext), static value => value.ToHttpResponse());

    [HttpPost("login-widget")]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> TelegramLoginWidget([FromBody] TelegramLoginWidgetHttpRequest request) =>
        HandleOk(request.ToCommand(HttpContext), static value => value.ToHttpResponse());

    [Authorize]
    [HttpPost("link")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> LinkTelegram([FromCurrentUser] Guid userId, [FromBody] TelegramAuthHttpRequest request) =>
        HandleOk(request.ToLinkCommand(userId), static value => value.ToHttpResponse());

    [HttpPost("bot/auth")]
    [RequireTelegramBotSecret]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> TelegramBotAuth([FromBody] TelegramBotAuthHttpRequest request) =>
        HandleOk(request.ToCommand(HttpContext), static value => value.ToHttpResponse());
}
