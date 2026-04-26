using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Consumptions.Mappings;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;
using FoodDiary.Presentation.Api.Features.Consumptions.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Consumptions;

[ApiController]
[Route("api/v{version:apiVersion}/consumptions")]
public class ConsumptionsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("overview")]
    [ProducesResponseType<ConsumptionOverviewHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetOverview([FromCurrentUser] Guid userId, [FromQuery] GetConsumptionsOverviewHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<ConsumptionHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetConsumptionsHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ConsumptionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id, [FromCurrentUser] Guid userId) =>
        HandleOk(id.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpPost]
    [EnableIdempotency]
    [ProducesResponseType<ConsumptionHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateConsumptionHttpRequest request) =>
        HandleCreated(
            request.ToCommand(userId),
            nameof(GetById),
            static value => new { id = value.Id },
            static value => value.ToHttpResponse());

    [HttpPatch("{id:guid}")]
    [ProducesResponseType<ConsumptionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateConsumptionHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/repeat")]
    [ProducesResponseType<ConsumptionHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Repeat(Guid id, [FromCurrentUser] Guid userId, [FromBody] RepeatMealHttpRequest request) =>
        HandleCreated(
            request.ToRepeatCommand(userId, id),
            nameof(GetById),
            static value => new { id = value.Id },
            static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));
}
