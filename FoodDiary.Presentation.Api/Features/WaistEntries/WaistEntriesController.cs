using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.WaistEntries;

[ApiController]
[Route("api/v{version:apiVersion}/waist-entries")]
public class WaistEntriesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<WaistEntryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetWaistEntriesHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpGet("latest")]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetLatest([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToLatestQuery(), static value => value?.ToHttpResponse());

    [HttpGet("summary")]
    [ProducesResponseType<List<WaistEntrySummaryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetSummary([FromCurrentUser] Guid userId, [FromQuery] GetWaistSummariesHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpPost]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateWaistEntryHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateWaistEntryHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));
}

