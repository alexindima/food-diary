using System.ComponentModel.DataAnnotations;
using FoodDiary.Mediator;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Commands.SaveCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlan;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.GetCatalogMealPlans;
using FoodDiary.Modules.MealPlanning.Application.MealPlans.Queries.SearchCatalogRecipes;
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
        HandleOk(new GetCatalogMealPlansQuery(query.Page, query.Limit), static items => items.ToHttpResponse());

    [HttpGet("recipes")]
    [ProducesResponseType<IReadOnlyList<CatalogRecipeHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> SearchRecipes([FromQuery, MaxLength(256)] string? search = null, [FromQuery, Range(1, 100)] int limit = 100) =>
        HandleOk(new SearchCatalogRecipesQuery(search, limit), static items => items.Select(item => new CatalogRecipeHttpResponse(item.Id, item.Name, item.Servings)).ToList());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetById(Guid id) =>
        HandleOk(new GetCatalogMealPlanQuery(id), static plan => plan.ToHttpResponse());

    [HttpPost]
    [EnableIdempotency]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status201Created)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create([FromBody] SaveCatalogMealPlanHttpRequest request) =>
        HandleCreated(ToCommand(id: null, request), static plan => plan.ToHttpResponse());

    [HttpPut("{id:guid}")]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(Guid id, [FromBody] SaveCatalogMealPlanHttpRequest request) =>
        HandleOk(ToCommand(id, request), static plan => plan.ToHttpResponse());

    private static SaveCatalogMealPlanCommand ToCommand(Guid? id, SaveCatalogMealPlanHttpRequest request) =>
        new(id, request.Name, request.Description, request.DietType, request.DurationDays,
            request.TargetCaloriesPerDay, request.IsPublished,
            [.. request.Days.Select(day => day is null
                ? new CatalogDayInput(DayNumber: 0, [])
                : new CatalogDayInput(day.DayNumber,
                    [.. day.Meals.Select(meal => meal is null
                        ? new CatalogMealInput(string.Empty, Guid.Empty, Servings: 0)
                        : new CatalogMealInput(meal.MealType, meal.RecipeId, meal.Servings))]))]);
}
