using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Cycles.Commands.CreateCycle;
using FoodDiary.Application.Cycles.Commands.UpsertCycleDay;
using FoodDiary.Application.Cycles.Mappings;
using FoodDiary.Application.Cycles.Queries.GetCurrentCycle;
using FoodDiary.Contracts.Cycles;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/cycles")]
public class CyclesController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent()
    {
        var query = new GetCurrentCycleQuery(CurrentUserId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCycleRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{cycleId:guid}/days")]
    public async Task<IActionResult> UpsertDay(Guid cycleId, [FromBody] UpsertCycleDayRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid, cycleId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
