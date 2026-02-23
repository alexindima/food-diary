using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Web.Api.Controllers;
using FoodDiary.Web.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Web.Api.Features.WaistEntries;

[ApiController]
[Route("api/waist-entries")]
public class WaistEntriesController(ISender mediator) : AuthorizedController(mediator) {
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
        var query = new GetWaistEntriesQuery(userId, dateFrom, dateTo, limit, descending);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetLatestWaistEntryQuery(userId);
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

        var query = new GetWaistSummariesQuery(userId, dateFrom, dateTo, quantizationDays);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWaistEntryRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWaistEntryRequest request) {
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

        var command = new DeleteWaistEntryCommand(
            userId,
            new WaistEntryId(id));

        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
