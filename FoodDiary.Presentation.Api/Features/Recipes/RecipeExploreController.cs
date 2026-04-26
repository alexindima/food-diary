using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Recipes.Mappings;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using FoodDiary.Presentation.Api.Features.Recipes.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Recipes;

[ApiController]
[Route("api/v{version:apiVersion}/recipes/explore")]
public class RecipeExploreController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<RecipeHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> Explore([FromCurrentUser] Guid userId, [FromQuery] ExploreRecipesHttpQuery query) =>
        HandleOk(query.ToExploreQuery(userId), static value => value.ToHttpResponse());
}
