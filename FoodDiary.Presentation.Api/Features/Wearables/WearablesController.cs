using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Wearables.Mappings;
using FoodDiary.Presentation.Api.Features.Wearables.Requests;
using FoodDiary.Presentation.Api.Features.Wearables.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Wearables;

[ApiController]
[Route("api/v{version:apiVersion}/wearables")]
public class WearablesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("connections")]
    [ProducesResponseType<IReadOnlyList<WearableConnectionHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetConnections([FromCurrentUser] Guid userId) =>
        HandleOk(WearableHttpMappings.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("{provider}/auth-url")]
    [ProducesResponseType<WearableAuthUrlHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAuthUrl(string provider, [FromQuery] string state) =>
        HandleOk(WearableHttpMappings.ToAuthUrlQuery(provider, state),
            static url => new WearableAuthUrlHttpResponse(url));

    [HttpPost("{provider}/connect")]
    [ProducesResponseType<WearableConnectionHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Connect(
        [FromCurrentUser] Guid userId,
        string provider,
        [FromBody] ConnectWearableHttpRequest request) =>
        HandleOk(request.ToCommand(userId, provider), static value => value.ToHttpResponse());

    [HttpDelete("{provider}/disconnect")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Disconnect(
        [FromCurrentUser] Guid userId,
        string provider) =>
        HandleNoContent(WearableHttpMappings.ToDisconnectCommand(userId, provider));

    [HttpPost("{provider}/sync")]
    [ProducesResponseType<WearableDailySummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Sync(
        [FromCurrentUser] Guid userId,
        string provider,
        [FromQuery] DateTime date) =>
        HandleOk(WearableHttpMappings.ToSyncCommand(userId, provider, date), static value => value.ToHttpResponse());

    [HttpGet("daily-summary")]
    [ProducesResponseType<WearableDailySummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetDailySummary(
        [FromCurrentUser] Guid userId,
        [FromQuery] DateTime date) =>
        HandleOk(WearableHttpMappings.ToDailySummaryQuery(userId, date), static value => value.ToHttpResponse());
}
