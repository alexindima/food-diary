using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.WeightEntries.Mappings;
using FoodDiary.Presentation.Api.Features.WeightEntries.Requests;
using FoodDiary.Presentation.Api.Features.WeightEntries.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.WeightEntries;

[ApiController]
[Route("api/weight-entries")]
public class WeightEntriesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<WeightEntryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetWeightEntriesHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpGet("latest")]
    [ProducesResponseType<WeightEntryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetLatest([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToLatestQuery(), static value => value?.ToHttpResponse());

    [HttpGet("summary")]
    [ProducesResponseType<List<WeightEntrySummaryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetSummary([FromCurrentUser] Guid userId, [FromQuery] GetWeightSummariesHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpPost]
    [ProducesResponseType<WeightEntryHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateWeightEntryHttpRequest request) =>
        HandleCreated(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<WeightEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateWeightEntryHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));
}

