using FoodDiary.Mediator;
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
    public Task<IActionResult> GetPage([FromQuery] PublicRecipesHttpQuery query) =>
        HandleOk(query.ToQuery(),
            static page => page.ToHttpResponse());

    [HttpGet("categories")]
    [ProducesResponseType<IReadOnlyList<string>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetCategories([FromQuery] string? search, [FromQuery] string? language) =>
        HandleOk(search.ToPublicCategoriesQuery(language), static categories => categories);

    [HttpGet("{id:guid}")]
    [ProducesResponseType<PublicRecipeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) => HandleOk(id.ToPublicRecipeQuery(), static recipe => recipe.ToHttpResponse());
}
