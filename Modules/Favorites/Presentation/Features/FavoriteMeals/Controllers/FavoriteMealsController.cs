using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteMeals.Mappings;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Favorites.Presentation.Features.FavoriteMeals.Requests;
using FoodDiary.Modules.Favorites.Presentation.Contracts.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.GetFavoriteMealPage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Favorites.Presentation.Features.FavoriteMeals.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/favorite-meals")]
public sealed class FavoriteMealsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<FavoriteMealHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpGet("page")]
    [ProducesResponseType<PagedHttpResponse<FavoriteMealHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetPage([FromCurrentUser] Guid userId, [FromQuery] GetFavoriteMealPageHttpQuery query) =>
        HandleOk(new GetFavoriteMealPageQuery(userId, query.Page, query.Limit, query.Search),
            static value => new PagedHttpResponse<FavoriteMealHttpResponse>(value.Data.Select(item => item.ToHttpResponse()).ToArray(),
                value.Page, value.Limit, value.TotalPages, value.TotalItems));

    [HttpGet("check/{mealId:guid}")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public Task<IActionResult> IsFavorite(Guid mealId, [FromCurrentUser] Guid userId) =>
        HandleOk(mealId.ToIsFavoriteQuery(userId), static value => value);

    [HttpPost]
    [ProducesResponseType<FavoriteMealHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Add([FromCurrentUser] Guid userId, [FromBody] AddFavoriteMealHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Remove(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));
}
