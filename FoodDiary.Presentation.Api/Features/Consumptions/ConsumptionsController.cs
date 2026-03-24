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
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromCurrentUser] UserId userId) {
        var result = await Mediator.Send(id.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateConsumptionHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] UserId userId, [FromBody] UpdateConsumptionHttpRequest request) {
        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] UserId userId) {
        var command = id.ToDeleteCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToNoContentActionResult();
    }
}
