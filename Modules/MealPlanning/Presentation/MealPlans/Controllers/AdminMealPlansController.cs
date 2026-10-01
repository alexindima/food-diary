using System.ComponentModel.DataAnnotations;
using FoodDiary.Mediator;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Mappings;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Requests;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Responses;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Requests;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/admin/meal-plans")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminMealPlansController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MealPlanSummaryHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetPage([FromQuery] OffsetPaginationHttpQuery query) =>
        HandleOk(query.ToCatalogQuery(), static items => items.ToHttpResponse());

    [HttpGet("recipes")]
    [ProducesResponseType<IReadOnlyList<CatalogRecipeHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> SearchRecipes([FromQuery, MaxLength(256)] string? search = null, [FromQuery, Range(1, 100)] int limit = 100) =>
        HandleOk(search.ToCatalogRecipeSearchQuery(limit), static items => items.Select(item => new CatalogRecipeHttpResponse(item.Id, item.Name, item.Servings)).ToList());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) =>
        HandleOk(id.ToCatalogMealPlanQuery(), static plan => plan.ToHttpResponse());

    [HttpPost]
    [EnableIdempotency]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] SaveCatalogMealPlanHttpRequest request) =>
        HandleCreated(request.ToCatalogCommand(id: null), static plan => plan.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveCatalogMealPlanHttpRequest request) =>
        HandleOk(request.ToCatalogCommand(id), static plan => plan.ToHttpResponse());
}
