using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth/admin-sso")]
public sealed class AdminSsoController(ISender mediator) : BaseApiController(mediator) {
    [Authorize(Roles = PresentationRoleNames.Admin)]
    [HttpPost("start")]
    [ProducesResponseType<AdminSsoStartHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> AdminSsoStart([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToAdminSsoStartCommand(), static value => value.ToHttpResponse());

    [AllowAnonymous]
    [HttpPost("exchange")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> AdminSsoExchange([FromBody] AdminSsoExchangeHttpRequest request) =>
        HandleOk(request.ToCommand(HttpContext), static value => value.ToHttpResponse());
}
