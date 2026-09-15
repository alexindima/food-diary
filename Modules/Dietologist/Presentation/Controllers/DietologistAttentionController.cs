using System.ComponentModel.DataAnnotations;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Dietologist.Presentation.Mappings;
using FoodDiary.Modules.Dietologist.Presentation.Requests;
using FoodDiary.Modules.Dietologist.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Dietologist.Presentation.Controllers;

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
        [Required, MaxLength(DietologistRequestLimits.MaximumSignalIdLength)] string signalId,
        [FromCurrentUser] Guid userId,
        [FromBody] SetAttentionSignalStateHttpRequest request) =>
        HandleNoContent(request.ToCommand(userId, signalId));
}
