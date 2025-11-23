using System;
using System.Threading.Tasks;
using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Mappings;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ConsumptionsController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null)
    {
        var query = new GetConsumptionsQuery(CurrentUserId, page, limit, dateFrom, dateTo);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var query = new GetConsumptionByIdQuery(CurrentUserId, new MealId(id));
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConsumptionRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConsumptionRequest request)
    {
        var command = request.ToCommand(CurrentUserGuid, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var command = new DeleteConsumptionCommand(CurrentUserId, new MealId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
