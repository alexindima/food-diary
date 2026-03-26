using FoodDiary.Presentation.Api.Authorization;
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

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/auth")]
public class AuthController(ISender mediator) : BaseApiController(mediator) {
    [HttpPost("register")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Register([FromBody] RegisterHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("login")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> Login([FromBody] LoginHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> Refresh([FromBody] RefreshTokenHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToAccessTokenHttpResponse());

    [HttpPost("restore")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> RestoreAccount([FromBody] RestoreAccountHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> VerifyEmail([FromBody] VerifyEmailHttpRequest request) =>
        HandleNoContent(request.ToCommand());

    [Authorize]
    [HttpPost("verify-email/resend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ResendVerifyEmail([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToResendVerificationCommand());

    [HttpPost("password-reset/request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetHttpRequest request) =>
        HandleNoContent(request.ToCommand());

    [HttpPost("password-reset/confirm")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("telegram/verify")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> TelegramVerify([FromBody] TelegramAuthHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("telegram/login-widget")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> TelegramLoginWidget([FromBody] TelegramLoginWidgetHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [Authorize]
    [HttpPost("telegram/link")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> LinkTelegram([FromCurrentUser] Guid userId, [FromBody] TelegramAuthHttpRequest request) =>
        HandleOk(request.ToLinkCommand(userId), static value => value.ToHttpResponse());

    [HttpPost("telegram/bot/auth")]
    [RequireTelegramBotSecret]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> TelegramBotAuth([FromBody] TelegramBotAuthHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [Authorize(Roles = PresentationRoleNames.Admin)]
    [HttpPost("admin-sso/start")]
    [ProducesResponseType<AdminSsoStartHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> AdminSsoStart([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToAdminSsoStartCommand(), static value => value.ToHttpResponse());

    [AllowAnonymous]
    [HttpPost("admin-sso/exchange")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    public Task<IActionResult> AdminSsoExchange([FromBody] AdminSsoExchangeHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());
}

