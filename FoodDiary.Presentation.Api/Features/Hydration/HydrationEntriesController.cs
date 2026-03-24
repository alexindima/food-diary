using FoodDiary.Application.Hydration.Commands.DeleteHydrationEntry;
using FoodDiary.Application.Hydration.Queries.GetHydrationDailyTotal;
using FoodDiary.Application.Hydration.Queries.GetHydrationEntries;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Hydration.Mappings;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Hydration;

[ApiController]
[Route("api/hydrations")]
public class HydrationEntriesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromQuery] DateTime? dateUtc = null) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var date = dateUtc ?? DateTime.UtcNow;
        var query = new GetHydrationEntriesQuery(userId, date);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromQuery] DateTime? dateUtc = null) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var date = dateUtc ?? DateTime.UtcNow;
        var query = new GetHydrationDailyTotalQuery(userId, date);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateHydrationEntryHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateHydrationEntryHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new DeleteHydrationEntryCommand(userId, new HydrationEntryId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
