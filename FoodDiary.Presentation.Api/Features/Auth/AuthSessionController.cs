using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthSessionController(ISender mediator, ILogger<AuthSessionController> logger) : BaseApiController(mediator) {
    private readonly ILogger<AuthSessionController> _logger = logger;

    [HttpPost("register")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> Register([FromBody] RegisterHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.register");

    [HttpPost("login")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> Login([FromBody] LoginHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.login");

    [HttpPost("google")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> GoogleLogin([FromBody] GoogleLoginHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.google-login");

    [HttpPost("refresh")]
    [EnableIdempotency]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> Refresh([FromBody] RefreshTokenHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.refresh");

    [HttpPost("restore")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> RestoreAccount([FromBody] RestoreAccountHttpRequest request) =>
        HandleObservedOk(request.ToCommand(), static value => value.ToHttpResponse(), _logger, "auth.restore-account");

    [HttpPost("verify-email")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> VerifyEmail([FromBody] VerifyEmailHttpRequest request) =>
        HandleObservedNoContent(request.ToCommand(), _logger, "auth.verify-email");

    [Authorize]
    [HttpPost("verify-email/resend")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> ResendVerifyEmail([FromCurrentUser] Guid userId, [FromBody] ResendEmailVerificationHttpRequest? request = null) =>
        HandleObservedNoContent(userId.ToResendVerificationCommand(request?.ClientOrigin), _logger, "auth.verify-email.resend", userId);
}
