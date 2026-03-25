using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
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
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetWaistEntriesHttpQuery query) {
        var result = await Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.Select(item => item.ToHttpResponse()).ToList());
    }

    [HttpGet("latest")]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetLatest([FromCurrentUser] Guid userId) {
        var result = await Send(userId.ToLatestQuery());
        return result.ToOkActionResult(this, static value => value?.ToHttpResponse());
    }

    [HttpGet("summary")]
    [ProducesResponseType<List<WaistEntrySummaryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSummary([FromCurrentUser] Guid userId, [FromQuery] GetWaistSummariesHttpQuery query) {
        var result = await Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.Select(item => item.ToHttpResponse()).ToList());
    }

    [HttpPost]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateWaistEntryHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<WaistEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateWaistEntryHttpRequest request) {
        var command = request.ToCommand(userId, id);
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) {
        var command = id.ToDeleteCommand(userId);
        var result = await Send(command);
        return result.ToNoContentActionResult();
    }
}

