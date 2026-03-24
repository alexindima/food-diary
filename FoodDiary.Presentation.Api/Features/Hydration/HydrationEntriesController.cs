using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Hydration.Mappings;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Hydration;

[ApiController]
[Route("api/hydrations")]
public class HydrationEntriesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetByDate([FromCurrentUser] Guid userId, [FromQuery] GetHydrationEntriesHttpQuery query) {
        var result = await Mediator.Send(query.ToEntriesQuery(userId));
        return result.ToOkActionResult(this, static value => value.Select(item => item.ToHttpResponse()).ToList());
    }

    [HttpGet("daily")]
    public async Task<IActionResult> GetDaily([FromCurrentUser] Guid userId, [FromQuery] GetHydrationEntriesHttpQuery query) {
        var result = await Mediator.Send(query.ToDailyQuery(userId));
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateHydrationEntryHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateHydrationEntryHttpRequest request) {
        var command = request.ToCommand(userId, id);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) {
        var command = id.ToDeleteCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToNoContentActionResult();
    }
}
