using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

[ApiController]
[Authorize(Roles = PresentationRoleNames.Dietologist)]
[Route("api/v{version:apiVersion}/dietologist/clients/attention")]
public sealed class DietologistAttentionController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<AttentionSignalHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAttentionSignals(
        [FromCurrentUser] Guid userId,
        [FromQuery] GetAttentionSignalsHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(signal => signal.ToHttpResponse()).ToList());

    [HttpPut("{signalId}/state")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> SetAttentionSignalState(
        string signalId,
        [FromCurrentUser] Guid userId,
        [FromBody] SetAttentionSignalStateHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId, signalId));
}
