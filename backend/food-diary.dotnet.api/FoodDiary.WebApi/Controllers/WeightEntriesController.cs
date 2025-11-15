using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.WeightEntries.Commands.CreateWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.DeleteWeightEntry;
using FoodDiary.Application.WeightEntries.Commands.UpdateWeightEntry;
using FoodDiary.Application.WeightEntries.Mappings;
using FoodDiary.Application.WeightEntries.Queries.GetLatestWeightEntry;
using FoodDiary.Application.WeightEntries.Queries.GetWeightEntries;
using FoodDiary.Contracts.WeightEntries;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/weight-entries")]
public class WeightEntriesController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int? limit = null,
        [FromQuery] string sort = "desc")
    {
        var descending = !string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase);
        var query = new GetWeightEntriesQuery(CurrentUserId, dateFrom, dateTo, limit, descending);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest()
    {
        var query = new GetLatestWeightEntryQuery(CurrentUserId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWeightEntryRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWeightEntryRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteWeightEntryCommand(
            CurrentUserId,
            new WeightEntryId(id));

        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
