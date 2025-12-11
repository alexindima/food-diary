using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Hydration.Commands.CreateHydrationEntry;
using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Commands.UpdateHydrationEntry;
using FoodDiary.Application.Hydration.Mappings;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Contracts.Hydration;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/hydrations")]
public class HydrationEntriesController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateTime? dateUtc = null)
    {
        var date = dateUtc ?? DateTime.UtcNow;
        var query = new GetHydrationEntriesQuery(CurrentUserId, date);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] DateTime? dateUtc = null)
    {
        var date = dateUtc ?? DateTime.UtcNow;
        var query = new GetHydrationDailyTotalQuery(CurrentUserId, date);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHydrationEntryRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHydrationEntryRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteHydrationEntryCommand(CurrentUserId, new HydrationEntryId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
