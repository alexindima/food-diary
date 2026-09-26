using FoodDiary.Mediator;
using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipe;
using FoodDiary.Modules.Recipes.Application.Queries.GetPublicRecipes;
using FoodDiary.Modules.Recipes.Presentation.Mappings;
using FoodDiary.Modules.Recipes.Presentation.Requests;
using FoodDiary.Modules.Recipes.Presentation.Responses;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Recipes.Presentation.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/recipes/public")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PublicRecipesController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<PublicRecipeHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAll([FromQuery] PublicRecipesHttpQuery query) =>
        HandleOk(new GetPublicRecipesQuery(query.Page, query.Limit, query.Search, query.Category, query.MaxTotalTime),
            static page => page.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PublicRecipeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) => HandleOk(new GetPublicRecipeQuery(id), static recipe => recipe.ToHttpResponse());
}
