using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Auth.Mappings;
using FoodDiary.Presentation.Api.Features.Auth.Requests;
using FoodDiary.Presentation.Api.Features.Auth.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Auth;

[ApiController]
[Route("api/v{version:apiVersion}/auth/impersonation")]
[RequestSizeLimit(AuthRequestLimits.MaxPayloadBytes)]
[RejectOversizedRequest(AuthRequestLimits.MaxPayloadBytes)]
[ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuthImpersonationController(ISender mediator) : BaseApiController(mediator) {
    [AllowAnonymous]
    [HttpPost("exchange")]
    [ProducesResponseType<ExchangeImpersonationHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status401Unauthorized)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
    public Task<IActionResult> ExchangeImpersonation([FromBody] ExchangeImpersonationHttpRequest request) =>
        HandleOk(request.ToCommand(), static accessToken => new ExchangeImpersonationHttpResponse(accessToken));
}
