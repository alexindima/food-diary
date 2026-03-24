using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Queries.GetConsumptionById;
using FoodDiary.Application.Consumptions.Queries.GetConsumptions;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Consumptions.Mappings;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Consumptions;

[ApiController]
[Route("api/[controller]")]
public class ConsumptionsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetConsumptionsQuery(userId, page, limit, dateFrom, dateTo);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetConsumptionByIdQuery(userId, new MealId(id));
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateConsumptionHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateConsumptionHttpRequest request) {
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

        var command = new DeleteConsumptionCommand(userId, new MealId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
