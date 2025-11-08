using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Services;
using FoodDiary.Contracts.Authentication;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthenticationService _authService;

    public AuthController(AuthenticationService authService) => _authService = authService;

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request.Email, request.Password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request.Email, request.Password);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenRequest request)
    {
        try
        {
            var accessToken = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(new { accessToken });
        }
        catch (Exception ex)
        {
            return Unauthorized(new { message = ex.Message });
        }
    }
}
