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
    public async Task<IActionResult> GetAll([FromCurrentUser] UserId userId, [FromQuery] GetConsumptionsHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromCurrentUser] UserId userId) {
        var query = new GetConsumptionByIdQuery(userId, new MealId(id));
        var result = await Mediator.Send(query);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateConsumptionHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] UserId userId, [FromBody] UpdateConsumptionHttpRequest request) {
        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] UserId userId) {
        var command = new DeleteConsumptionCommand(userId, new MealId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
