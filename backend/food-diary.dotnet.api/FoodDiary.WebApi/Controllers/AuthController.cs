using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Authentication.Mappings;
using FoodDiary.Contracts.Authentication;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController(ISender mediator) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var command = request.ToCommand();
        var result = await mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var command = request.ToCommand();
        var result = await mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        var command = request.ToCommand();
        var result = await mediator.Send(command);

        if (result.IsSuccess)
        {
            return Ok(new { accessToken = result.Value });
        }

        return result.ToActionResult();
    }
}
