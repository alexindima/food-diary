using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.WaistEntries;

[ApiController]
[Route("api/waist-entries")]
public class WaistEntriesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetWaistEntriesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.Select(item => item.ToHttpResponse()).ToList());
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatest([FromCurrentUser] Guid userId) {
        var result = await Mediator.Send(userId.ToLatestQuery());
        return result.ToOkActionResult(this, static value => value?.ToHttpResponse());
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromCurrentUser] Guid userId, [FromQuery] GetWaistSummariesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.Select(item => item.ToHttpResponse()).ToList());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateWaistEntryHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateWaistEntryHttpRequest request) {
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
