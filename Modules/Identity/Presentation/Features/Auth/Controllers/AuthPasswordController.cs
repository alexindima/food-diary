using FoodDiary.Modules.Identity.Presentation.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;

using FoodDiary.Modules.Identity.Presentation.Features.Auth.Requests;
using FoodDiary.Modules.Identity.Presentation.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Identity.Presentation.Features.Auth.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/auth/password-reset")]
[EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
[RequestSizeLimit(AuthRequestLimits.MaxPayloadBytes)]
[RejectOversizedRequest(AuthRequestLimits.MaxPayloadBytes)]
[ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthPasswordController(ISender mediator) : BaseApiController(mediator) {
    [AllowAnonymous]
    [HttpPost("request")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> RequestPasswordReset([FromBody] RequestPasswordResetHttpRequest request) =>
        HandleNoContent(request.ToCommand());

    [AllowAnonymous]
    [HttpPost("confirm")]
    [ProducesResponseType<AuthenticationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ConfirmPasswordReset([FromBody] ConfirmPasswordResetHttpRequest request) =>
        HandleOk(request.ToCommand(), static value => value.ToHttpResponse());
}
