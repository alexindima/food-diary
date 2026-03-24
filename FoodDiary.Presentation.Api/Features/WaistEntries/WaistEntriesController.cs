using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.WaistEntries;

[ApiController]
[Route("api/waist-entries")]
public class WaistEntriesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll([FromCurrentUser] UserId userId, [FromQuery] GetWaistEntriesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        if (result.IsFailure) {
            return result.ToActionResult();
        }

        return Ok(result.Value.Select(item => item.ToHttpResponse()).ToList());
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromCurrentUser] UserId userId) {
        var query = new GetLatestWaistEntryQuery(userId);
        var result = await Mediator.Send(query);
        if (result.IsFailure) {
            return result.ToActionResult();
        }

        return Ok(result.Value?.ToHttpResponse());
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromCurrentUser] UserId userId, [FromQuery] GetWaistSummariesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        if (result.IsFailure) {
            return result.ToActionResult();
        }

        return Ok(result.Value.Select(item => item.ToHttpResponse()).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateWaistEntryHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        if (result.IsFailure) {
            return result.ToActionResult();
        }

        return Ok(result.Value.ToHttpResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] UserId userId, [FromBody] UpdateWaistEntryHttpRequest request) {
        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        if (result.IsFailure) {
            return result.ToActionResult();
        }

        return Ok(result.Value.ToHttpResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] UserId userId) {
        var command = new DeleteWaistEntryCommand(
            userId,
            new WaistEntryId(id));

        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
