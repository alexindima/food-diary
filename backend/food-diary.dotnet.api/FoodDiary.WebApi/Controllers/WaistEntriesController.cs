using System;
using System.Threading.Tasks;
using FoodDiary.Application.WaistEntries.Commands.CreateWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.DeleteWaistEntry;
using FoodDiary.Application.WaistEntries.Commands.UpdateWaistEntry;
using FoodDiary.Application.WaistEntries.Mappings;
using FoodDiary.Application.WaistEntries.Queries.GetLatestWaistEntry;
using FoodDiary.Application.WaistEntries.Queries.GetWaistEntries;
using FoodDiary.Application.WaistEntries.Queries.GetWaistSummaries;
using FoodDiary.Contracts.WaistEntries;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/waist-entries")]
public class WaistEntriesController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int? limit = null,
        [FromQuery] string sort = "desc")
    {
        var descending = !string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase);
        var query = new GetWaistEntriesQuery(CurrentUserId, dateFrom, dateTo, limit, descending);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var query = new GetLatestWaistEntryQuery(CurrentUserId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] int quantizationDays = 1)
    {
        var query = new GetWaistSummariesQuery(CurrentUserId, dateFrom, dateTo, quantizationDays);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWaistEntryRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWaistEntryRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteWaistEntryCommand(
            CurrentUserId,
            new WaistEntryId(id));

        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
