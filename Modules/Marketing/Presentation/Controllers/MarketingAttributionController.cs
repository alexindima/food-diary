using FoodDiary.Presentation.Api.Telemetry;
using FoodDiary.Modules.Marketing.Presentation.Mappings;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.Marketing.Presentation.Requests;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Marketing.Presentation.Controllers;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/v{version:apiVersion}/marketing/attribution-events")]
[SuppressRequestAccessLog]
[EnableRateLimiting(PresentationPolicyNames.MarketingAttributionRateLimitPolicyName)]
public sealed class MarketingAttributionController(ISender mediator) : BaseApiController(mediator) {
    public const int MaxPayloadBytes = 16 * 1024;

    [AllowAnonymous]
    [HttpPost]
    [RequestSizeLimit(MaxPayloadBytes)]
    [RejectOversizedRequest(MaxPayloadBytes)]
    [EnableIdempotency(requireKey: true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> Create(
        [FromHeader(Name = "Idempotency-Key")] Guid eventId,
        [FromBody] MarketingAttributionHttpRequest request) =>
        HandleNoContent(request.ToCommand(eventId));

    [Authorize]
    [HttpPost("signup")]
    [RequestSizeLimit(MaxPayloadBytes)]
    [RejectOversizedRequest(MaxPayloadBytes)]
    [EnableIdempotency(requireKey: true)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> CreateSignup(
        [FromCurrentUser] Guid userId,
        [FromHeader(Name = "Idempotency-Key")] Guid eventId,
        [FromBody] MarketingSignupAttributionHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId, eventId));
}
