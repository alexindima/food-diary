using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Options;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
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
    public async Task<IActionResult> Register(RegisterHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);

        return result.IsSuccess ? Ok(new { accessToken = result.Value }) : result.ToActionResult();
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreAccount(RestoreAccountHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("verify-email/resend")]
    public async Task<IActionResult> ResendVerifyEmail([FromCurrentUser] UserId userId) {
        var command = new ResendEmailVerificationCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(RequestPasswordResetHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmPasswordReset(ConfirmPasswordResetHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("telegram/verify")]
    public async Task<IActionResult> TelegramVerify(TelegramAuthHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("telegram/login-widget")]
    public async Task<IActionResult> TelegramLoginWidget(TelegramLoginWidgetHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("telegram/link")]
    public async Task<IActionResult> LinkTelegram([FromCurrentUser] UserId userId, TelegramAuthHttpRequest request) {
        var command = request.ToLinkCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("telegram/bot/auth")]
    public async Task<IActionResult> TelegramBotAuth(
        [FromHeader(Name = "X-Telegram-Bot-Secret")]
        string? secret,
        TelegramBotAuthHttpRequest request) {
        if (string.IsNullOrWhiteSpace(_telegramBotOptions.ApiSecret)) {
            return StatusCode(StatusCodes.Status500InternalServerError, new {
                error = "Authentication.TelegramBotNotConfigured",
                message = "Telegram bot authentication is not configured."
            });
        }

        if (!string.Equals(secret, _telegramBotOptions.ApiSecret, StringComparison.Ordinal)) {
            return Unauthorized(new {
                error = "Authentication.TelegramBotInvalidSecret",
                message = "Telegram bot secret is invalid."
            });
        }

        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [Authorize(Roles = RoleNames.Admin)]
    [HttpPost("admin-sso/start")]
    public async Task<IActionResult> AdminSsoStart([FromCurrentUser] UserId userId) {
        var command = new AdminSsoStartCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("admin-sso/exchange")]
    public async Task<IActionResult> AdminSsoExchange(AdminSsoExchangeHttpRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
