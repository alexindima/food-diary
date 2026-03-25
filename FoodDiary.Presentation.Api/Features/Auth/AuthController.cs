using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Options;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Application.Common.Abstractions.Result;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ISender mediator,
    IOptions<TelegramBotAuthOptions> telegramBotOptions) : BaseApiController(mediator) {
    private readonly TelegramBotAuthOptions _telegramBotOptions = telegramBotOptions.Value;

    [HttpPost("register")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Register(RegisterHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost("login")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Login(LoginHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost("refresh")]
    [ProducesResponseType<AccessTokenHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Refresh(RefreshTokenHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToAccessTokenHttpResponse());
    }

    [HttpPost("restore")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RestoreAccount(RestoreAccountHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> VerifyEmail(VerifyEmailHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToNoContentActionResult();
    }

    [Authorize]
    [HttpPost("verify-email/resend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ResendVerifyEmail([FromCurrentUser] Guid userId) {
        var command = userId.ToResendVerificationCommand();
        var result = await Send(command);
        return result.ToNoContentActionResult();
    }

    [HttpPost("password-reset/request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RequestPasswordReset(RequestPasswordResetHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToNoContentActionResult();
    }

    [HttpPost("password-reset/confirm")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ConfirmPasswordReset(ConfirmPasswordResetHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost("telegram/verify")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TelegramVerify(TelegramAuthHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost("telegram/login-widget")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TelegramLoginWidget(TelegramLoginWidgetHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [Authorize]
    [HttpPost("telegram/link")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> LinkTelegram([FromCurrentUser] Guid userId, TelegramAuthHttpRequest request) {
        var command = request.ToLinkCommand(userId);
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost("telegram/bot/auth")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TelegramBotAuth(
        [FromHeader(Name = "X-Telegram-Bot-Secret")]
        string? secret,
        TelegramBotAuthHttpRequest request) {
        if (string.IsNullOrWhiteSpace(_telegramBotOptions.ApiSecret)) {
            return new Error(
                "Authentication.TelegramBotNotConfigured",
                "Telegram bot authentication is not configured.")
                .ToErrorActionResult(StatusCodes.Status500InternalServerError);
        }

        if (!SecretComparison.FixedTimeEquals(_telegramBotOptions.ApiSecret, secret)) {
            return new Error(
                "Authentication.TelegramBotInvalidSecret",
                "Telegram bot secret is invalid.")
                .ToErrorActionResult(StatusCodes.Status401Unauthorized);
        }

        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [Authorize(Roles = PresentationRoleNames.Admin)]
    [HttpPost("admin-sso/start")]
    [ProducesResponseType<AdminSsoStartHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdminSsoStart([FromCurrentUser] Guid userId) {
        var command = userId.ToAdminSsoStartCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [AllowAnonymous]
    [HttpPost("admin-sso/exchange")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AdminSsoExchange(AdminSsoExchangeHttpRequest request) {
        var command = request.ToCommand();
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}

