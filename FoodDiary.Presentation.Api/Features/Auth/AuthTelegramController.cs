using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/auth/telegram")]
public sealed class AuthTelegramController(ISender mediator, ILogger<AuthTelegramController> logger) : BaseApiController(mediator) {
    private readonly ILogger<AuthTelegramController> _logger = logger;

    [HttpPost("verify")]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> TelegramVerify([FromBody] TelegramAuthHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.telegram.verify");

    [HttpPost("login-widget")]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> TelegramLoginWidget([FromBody] TelegramLoginWidgetHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.telegram.login-widget");

    [Authorize]
    [HttpPost("link")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> LinkTelegram([FromCurrentUser] Guid userId, [FromBody] TelegramAuthHttpRequest request) =>
        HandleObservedOk(request.ToLinkCommand(userId), static value => value.ToHttpResponse(), _logger, "auth.telegram.link", userId);

    [HttpPost("bot/auth")]
    [RequireTelegramBotSecret]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> TelegramBotAuth([FromBody] TelegramBotAuthHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.telegram.bot-auth");
}
