using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Cycles.Presentation.Mappings;
using FoodDiary.Modules.Cycles.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Cycles.Presentation.Requests;
using FoodDiary.Modules.Cycles.Presentation.Contracts.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Cycles.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/cycles")]
public sealed class CycleLogsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPut("{cycleProfileId:guid}/days")]
    [ProducesResponseType<CycleLogDayHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpsertDay(Guid cycleProfileId, [FromCurrentUser] Guid userId, [FromBody] UpsertCycleDayHttpRequest request) =>
        HandleOk(request.ToCommand(userId, cycleProfileId), static value => value.ToHttpResponse());

    [HttpDelete("{cycleProfileId:guid}/days")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> ClearDay(Guid cycleProfileId, [FromCurrentUser] Guid userId, [FromQuery] DateTime date) =>
        HandleNoContent(cycleProfileId.ToClearDayCommand(userId, date));

    [HttpPut("{cycleProfileId:guid}/factors")]
    [ProducesResponseType<CycleHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpsertFactor(Guid cycleProfileId, [FromCurrentUser] Guid userId, [FromBody] UpsertCycleFactorHttpRequest request) =>
        HandleOk(request.ToCommand(userId, cycleProfileId), static value => value.ToHttpResponse());
}
