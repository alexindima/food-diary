using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Hydration.Mappings;
using FoodDiary.Presentation.Api.Features.Hydration.Requests;
using FoodDiary.Presentation.Api.Features.Hydration.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Hydration;

[ApiController]
[Route("api/hydrations")]
public class HydrationEntriesController(ISender mediator, IPresentationClock clock) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<HydrationEntryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetByDate([FromCurrentUser] Guid userId, [FromQuery] GetHydrationEntriesHttpQuery query) =>
        HandleOk(query.ToEntriesQuery(userId, clock.UtcNow), static value => value.Select(item => item.ToHttpResponse()).ToList());

    [HttpGet("daily")]
    [ProducesResponseType<HydrationDailyHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetDaily([FromCurrentUser] Guid userId, [FromQuery] GetHydrationEntriesHttpQuery query) =>
        HandleOk(query.ToDailyQuery(userId, clock.UtcNow), static value => value.ToHttpResponse());

    [HttpPost]
    [ProducesResponseType<HydrationEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateHydrationEntryHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<HydrationEntryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateHydrationEntryHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));
}

