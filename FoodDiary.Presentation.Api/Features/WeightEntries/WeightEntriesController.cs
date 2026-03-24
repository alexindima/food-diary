using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Application.WeightEntries.Queries.GetWeightSummaries;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.WeightEntries;

[ApiController]
[Route("api/weight-entries")]
public class WeightEntriesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll([FromCurrentUser] UserId userId, [FromQuery] GetWeightEntriesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromCurrentUser] UserId userId) {
        var query = new GetLatestWeightEntryQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromCurrentUser] UserId userId, [FromQuery] GetWeightSummariesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateWeightEntryHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] UserId userId, [FromBody] UpdateWeightEntryHttpRequest request) {
        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] UserId userId) {
        var command = new DeleteWeightEntryCommand(
            userId,
            new WeightEntryId(id));

        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
