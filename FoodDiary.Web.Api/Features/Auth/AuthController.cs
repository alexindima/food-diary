using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Application.Authentication.Commands.ResendEmailVerification;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Options;
using FoodDiary.Web.Api.Controllers;
using FoodDiary.Web.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FoodDiary.Web.Api.Features.Auth;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ISender mediator,
    IOptions<TelegramBotOptions> telegramBotOptions) : BaseApiController(mediator) {
    private readonly TelegramBotOptions _telegramBotOptions = telegramBotOptions.Value;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);

        return result.IsSuccess ? Ok(new { accessToken = result.Value }) : result.ToActionResult();
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreAccount(RestoreAccountRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("verify-email")]
    public async Task<IActionResult> VerifyEmail(VerifyEmailRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("verify-email/resend")]
    public async Task<IActionResult> ResendVerifyEmail() {
        if (!TryGetAuthenticatedUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new ResendEmailVerificationCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("password-reset/request")]
    public async Task<IActionResult> RequestPasswordReset(RequestPasswordResetRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("password-reset/confirm")]
    public async Task<IActionResult> ConfirmPasswordReset(ConfirmPasswordResetRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("telegram/verify")]
    public async Task<IActionResult> TelegramVerify(TelegramAuthRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("telegram/login-widget")]
    public async Task<IActionResult> TelegramLoginWidget(TelegramLoginWidgetRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("telegram/link")]
    public async Task<IActionResult> LinkTelegram(TelegramAuthRequest request) {
        if (!TryGetAuthenticatedUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToLinkCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("telegram/bot/auth")]
    public async Task<IActionResult> TelegramBotAuth(
        [FromHeader(Name = "X-Telegram-Bot-Secret")]
        string? secret,
        TelegramBotAuthRequest request) {
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
    public async Task<IActionResult> AdminSsoStart() {
        if (!TryGetAuthenticatedUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new AdminSsoStartCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("admin-sso/exchange")]
    public async Task<IActionResult> AdminSsoExchange(AdminSsoExchangeRequest request) {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    private bool TryGetAuthenticatedUserId(out UserId userId) {
        var resolved = User.GetUserId();
        if (resolved is null || resolved.Value == UserId.Empty) {
            userId = default;
            return false;
        }

        userId = resolved.Value;
        return true;
    }
}
