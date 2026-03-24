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
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int? limit = null,
        [FromQuery] string sort = "desc") {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var descending = !string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase);
        var query = new GetWeightEntriesQuery(userId, dateFrom, dateTo, limit, descending);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetLatestWeightEntryQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] int quantizationDays = 1) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetWeightSummariesQuery(userId, dateFrom, dateTo, quantizationDays);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWeightEntryHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWeightEntryHttpRequest request) {
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

        var command = new DeleteWeightEntryCommand(
            userId,
            new WeightEntryId(id));

        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
