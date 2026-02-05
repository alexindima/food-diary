using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Application.Authentication.Commands.AdminSsoStart;
using FoodDiary.Contracts.Authentication;
using FoodDiary.Domain.Enums;
using FoodDiary.Infrastructure.Options;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(
    ISender mediator,
    IOptions<TelegramBotOptions> telegramBotOptions) : BaseApiController(mediator)
{
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

    [HttpPost("telegram/verify")]
    public async Task<IActionResult> TelegramVerify(TelegramAuthRequest request)
    {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("telegram/login-widget")]
    public async Task<IActionResult> TelegramLoginWidget(TelegramLoginWidgetRequest request)
    {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [Authorize]
    [HttpPost("telegram/link")]
    public async Task<IActionResult> LinkTelegram(TelegramAuthRequest request)
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var command = request.ToLinkCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("telegram/bot/auth")]
    public async Task<IActionResult> TelegramBotAuth(
        [FromHeader(Name = "X-Telegram-Bot-Secret")] string? secret,
        TelegramBotAuthRequest request)
    {
        if (string.IsNullOrWhiteSpace(_telegramBotOptions.ApiSecret))
        {
            return StatusCode(StatusCodes.Status500InternalServerError, new
            {
                error = "Authentication.TelegramBotNotConfigured",
                message = "Telegram bot authentication is not configured."
            });
        }

        if (!string.Equals(secret, _telegramBotOptions.ApiSecret, StringComparison.Ordinal))
        {
            return Unauthorized(new
            {
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
    public async Task<IActionResult> AdminSsoStart()
    {
        var userId = User.GetUserId();
        if (userId is null)
        {
            return Unauthorized();
        }

        var command = new AdminSsoStartCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [AllowAnonymous]
    [HttpPost("admin-sso/exchange")]
    public async Task<IActionResult> AdminSsoExchange(AdminSsoExchangeRequest request)
    {
        var command = request.ToCommand();
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
