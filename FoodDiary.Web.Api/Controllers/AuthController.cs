using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Contracts.Authentication;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender mediator) : BaseApiController(mediator) {
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
}
