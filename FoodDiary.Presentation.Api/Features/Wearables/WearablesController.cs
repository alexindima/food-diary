using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Wearables.Mappings;
using FoodDiary.Presentation.Api.Features.Wearables.Requests;
using FoodDiary.Presentation.Api.Features.Wearables.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Security;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Wearables;

[ApiController]
[Route("api/v{version:apiVersion}/wearables")]
public sealed class WearablesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("connections")]
    [ProducesResponseType<IReadOnlyList<WearableConnectionHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetConnections([FromCurrentUser] Guid userId) =>
        HandleOk(WearableHttpMappings.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("{provider}/auth-url")]
    [ProducesResponseType<WearableAuthUrlHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [BlockImpersonatedAccess]
    [EnableRateLimiting(PresentationPolicyNames.WearableRateLimitPolicyName)]
    public Task<IActionResult> GetAuthUrl(
        [FromCurrentUser] Guid userId,
        [Required, MaxLength(WearableRequestLimits.MaximumProviderLength)] string provider,
        [FromQuery, Required, MaxLength(WearableRequestLimits.MaximumOAuthStateLength)] string state) =>
        HandleOk(WearableHttpMappings.ToAuthUrlQuery(userId, provider, state),
            static url => new WearableAuthUrlHttpResponse(url));

    [HttpPost("{provider}/connect")]
    [ProducesResponseType<WearableConnectionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [BlockImpersonatedAccess]
    [EnableRateLimiting(PresentationPolicyNames.WearableRateLimitPolicyName)]
    [EnableIdempotency(requireKey: true)]
    public Task<IActionResult> Connect(
        [FromCurrentUser] Guid userId,
        [Required, MaxLength(WearableRequestLimits.MaximumProviderLength)] string provider,
        [FromBody] ConnectWearableHttpRequest request) =>
        HandleOk(
            request.ToCommand(userId, provider, GetIdempotencyRequestId(), GetIdempotencyRequestHash()),
            static value => value.ToHttpResponse());

    [HttpDelete("{provider}/disconnect")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [BlockImpersonatedAccess]
    public Task<IActionResult> Disconnect(
        [FromCurrentUser] Guid userId,
        [Required, MaxLength(WearableRequestLimits.MaximumProviderLength)] string provider) =>
        HandleNoContent(WearableHttpMappings.ToDisconnectCommand(userId, provider));

    [HttpPost("{provider}/sync")]
    [ProducesResponseType<WearableDailySummaryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [BlockImpersonatedAccess]
    [EnableRateLimiting(PresentationPolicyNames.WearableRateLimitPolicyName)]
    [EnableIdempotency(requireKey: true)]
    public Task<IActionResult> Sync(
        [FromCurrentUser] Guid userId,
        [Required, MaxLength(WearableRequestLimits.MaximumProviderLength)] string provider,
        [FromQuery] DateTime date) =>
        HandleOk(WearableHttpMappings.ToSyncCommand(userId, provider, date), static value => value.ToHttpResponse());

    [HttpGet("daily-summary")]
    [ProducesResponseType<WearableDailySummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetDailySummary(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime date) =>
        HandleOk(WearableHttpMappings.ToDailySummaryQuery(userId, date), static value => value.ToHttpResponse());

    private string GetIdempotencyRequestId() =>
        IdempotencyRequestContext.GetRequestId(HttpContext) ??
        throw new InvalidOperationException("Required idempotency request ID is unavailable.");

    private string GetIdempotencyRequestHash() =>
        IdempotencyRequestContext.GetRequestHash(HttpContext) ??
        throw new InvalidOperationException("Required idempotency request hash is unavailable.");
}
