using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Mappings;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteMeals.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.FavoriteMeals;

[ApiController]
[Route("api/v{version:apiVersion}/favorite-meals")]
public class FavoriteMealsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<FavoriteMealHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());

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
