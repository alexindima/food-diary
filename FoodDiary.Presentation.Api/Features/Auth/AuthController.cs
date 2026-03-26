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
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Register(RegisterHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("login")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> Login(LoginHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> Refresh(RefreshTokenHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToAccessTokenHttpResponse());

    [HttpPost("restore")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RestoreAccount(RestoreAccountHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> VerifyEmail(VerifyEmailHttpRequest request) =>
        HandleNoContent(request.ToCommand());

    [Authorize]
    [HttpPost("verify-email/resend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> ResendVerifyEmail([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToResendVerificationCommand());

    [HttpPost("password-reset/request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> RequestPasswordReset(RequestPasswordResetHttpRequest request) =>
        HandleNoContent(request.ToCommand());

    [HttpPost("password-reset/confirm")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> ConfirmPasswordReset(ConfirmPasswordResetHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("telegram/verify")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> TelegramVerify(TelegramAuthHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [HttpPost("telegram/login-widget")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> TelegramLoginWidget(TelegramLoginWidgetHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [Authorize]
    [HttpPost("telegram/link")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> LinkTelegram([FromCurrentUser] Guid userId, TelegramAuthHttpRequest request) =>
        HandleOk(request.ToLinkCommand(userId), static value => value.ToHttpResponse());

    [HttpPost("telegram/bot/auth")]
    [RequireTelegramBotSecret]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> TelegramBotAuth(TelegramBotAuthHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());

    [Authorize(Roles = PresentationRoleNames.Admin)]
    [HttpPost("admin-sso/start")]
    [ProducesResponseType<AdminSsoStartHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> AdminSsoStart([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToAdminSsoStartCommand(), static value => value.ToHttpResponse());

    [AllowAnonymous]
    [HttpPost("admin-sso/exchange")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> AdminSsoExchange(AdminSsoExchangeHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());
}

