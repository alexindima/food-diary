using FoodDiary.Modules.Recipes.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Modules.Recipes.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Recipes.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/recipes/explore")]
public sealed class RecipeExploreController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<RecipeHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Explore([FromCurrentUser] Guid userId, [FromQuery] ExploreRecipesHttpQuery query) =>
        HandleOk(query.ToExploreQuery(userId), static value => value.ToHttpResponse());
}
