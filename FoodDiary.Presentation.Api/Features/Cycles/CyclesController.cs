using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
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
    public async Task<IActionResult> GetCurrent() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetCurrentCycleQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCycleHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{cycleId:guid}/days")]
    public async Task<IActionResult> UpsertDay(Guid cycleId, [FromBody] UpsertCycleDayHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value, cycleId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
