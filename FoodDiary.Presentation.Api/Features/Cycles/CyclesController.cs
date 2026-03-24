using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
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
        var query = new GetCurrentCycleQuery(userId);
        var result = await Mediator.Send(query);
        return result.IsSuccess
            ? Ok(result.Value is null ? null : result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateCycleHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPut("{cycleId:guid}/days")]
    public async Task<IActionResult> UpsertDay(Guid cycleId, [FromCurrentUser] UserId userId, [FromBody] UpsertCycleDayHttpRequest request) {
        var command = request.ToCommand(userId.Value, cycleId);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }
}
