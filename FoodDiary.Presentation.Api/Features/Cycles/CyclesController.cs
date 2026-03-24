using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Cycles.Mappings;
using FoodDiary.Presentation.Api.Features.Cycles.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Cycles;

[ApiController]
[Route("api/cycles")]
public class CyclesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent([FromCurrentUser] UserId userId) {
        var result = await Mediator.Send(userId.ToCurrentQuery());
        return result.ToOkActionResult(this, static value => value is null ? null : value.ToHttpResponse());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateCycleHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPut("{cycleId:guid}/days")]
    public async Task<IActionResult> UpsertDay(Guid cycleId, [FromCurrentUser] UserId userId, [FromBody] UpsertCycleDayHttpRequest request) {
        var command = request.ToCommand(userId.Value, cycleId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
