using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Features.Recipes.Mappings;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Recipes;

[ApiController]
[Route("api/v{version:apiVersion}/recipes")]
public class RecipesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<RecipeHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId, [FromQuery] GetRecipesHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("overview")]
    [ProducesResponseType<RecipeOverviewHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetOverview([FromCurrentUser] Guid userId, [FromQuery] GetRecipesOverviewHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("recent")]
    [ProducesResponseType<List<RecipeHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetRecent([FromCurrentUser] Guid userId, [FromQuery] GetRecentRecipesHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<RecipeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id, [FromCurrentUser] Guid userId, [FromQuery] bool includePublic = true) =>
        HandleOk(id.ToQuery(userId, includePublic), static value => value.ToHttpResponse());

    [HttpPost]
    [EnableIdempotency]
    [ProducesResponseType<RecipeHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateRecipeHttpRequest request) =>
        HandleCreated(
            request.ToCommand(userId),
            nameof(GetById),
            static value => new { id = value.Id },
            static value => value.ToHttpResponse());

    [HttpPatch("{id:guid}")]
    [ProducesResponseType<RecipeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateRecipeHttpRequest request) =>
        HandleOk(request.ToCommand(userId, id), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));

    [HttpPost("{id:guid}/duplicate")]
    [EnableIdempotency]
    [ProducesResponseType<RecipeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Duplicate(Guid id, [FromCurrentUser] Guid userId) =>
        HandleOk(id.ToDuplicateCommand(userId), static value => value.ToHttpResponse());
}
