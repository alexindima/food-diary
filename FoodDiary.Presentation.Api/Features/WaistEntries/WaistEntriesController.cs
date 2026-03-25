using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.WaistEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WaistEntries.Requests;
using FoodDiary.Presentation.Api.Features.WaistEntries.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.WaistEntries;

[ApiController]
[Route("api/waist-entries")]
public class WaistEntriesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<WaistEntryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetWaistEntriesHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpGet("latest")]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetLatest([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToLatestQuery(), static value => value?.ToHttpResponse());

    [HttpGet("summary")]
    [ProducesResponseType<List<WaistEntrySummaryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> GetSummary([FromCurrentUser] Guid userId, [FromQuery] GetWaistSummariesHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpPost]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateWaistEntryHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateWaistEntryHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));
}

