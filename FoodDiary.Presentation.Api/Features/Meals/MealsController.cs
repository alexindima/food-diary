using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Meals.Mappings;
using FoodDiary.Presentation.Api.Features.Meals.Requests;
using FoodDiary.Presentation.Api.Features.Meals.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Meals;

[ApiController]
[Route("api/v{version:apiVersion}/meals")]
public sealed class MealsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("overview")]
    [ProducesResponseType<MealOverviewHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetOverview([FromCurrentUser] Guid userId, [FromQuery] GetMealsOverviewHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<MealHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetMealsHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MealHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id, [FromCurrentUser] Guid userId) =>
        HandleOk(id.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpPost]
    [EnableIdempotency]
    [ProducesResponseType<MealHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateMealHttpRequest request) =>
        HandleCreated(
            request.ToCommand(userId),
            nameof(GetById),
            static value => new { id = value.Id },
            static value => value.ToHttpResponse());

    [HttpPatch("{id:guid}")]
    [ProducesResponseType<MealHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateMealHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/repeat")]
    [ProducesResponseType<MealHttpResponse>(StatusCodes.Status201Created)]
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
