using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Mappings;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Requests;
using FoodDiary.Presentation.Api.Features.FavoriteRecipes.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.FavoriteRecipes;

[ApiController]
[Route("api/v{version:apiVersion}/favorite-recipes")]
public class FavoriteRecipesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<FavoriteRecipeHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpGet("check/{recipeId:guid}")]
    [ProducesResponseType<bool>(StatusCodes.Status200OK)]
    public Task<IActionResult> IsFavorite(Guid recipeId, [FromCurrentUser] Guid userId) =>
        HandleOk(recipeId.ToIsFavoriteQuery(userId), static value => value);

    [HttpPost]
    [ProducesResponseType<FavoriteRecipeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    public Task<IActionResult> Add([FromCurrentUser] Guid userId, [FromBody] AddFavoriteRecipeHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Remove(Guid id, [FromCurrentUser] Guid userId) =>
        HandleNoContent(id.ToDeleteCommand(userId));
}
