using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Mappings;
using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Responses;
using FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Policies;
using ShoppingListResponseMappings = FoodDiary.Modules.MealPlanning.Presentation.ShoppingLists.Mappings.ShoppingListHttpResponseMappings;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.MealPlanning.Presentation.MealPlans.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/meal-plans")]
public sealed class MealPlansController(ISender mediator) : AuthorizedController(mediator) {
    private const int MaximumDietTypeLength = 32;

    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<MealPlanSummaryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetPage(
        [FromCurrentUser] Guid userId,
        [FromQuery, MaxLength(MaximumDietTypeLength)] string? dietType = null,
        [FromQuery, OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int page = 1,
        [FromQuery, OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int limit = 20) =>
        HandleOk(userId.ToQuery(dietType, page, limit), static value => new PagedHttpResponse<MealPlanSummaryHttpResponse>(
            value.Data.ToHttpResponse(), value.Page, value.Limit, value.TotalPages, value.TotalItems));

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetById(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleOk(userId.ToGetByIdQuery(id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/adopt")]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status201Created)]
    [EnableIdempotency]
    public Task<IActionResult> Adopt(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleCreated(userId.ToAdoptCommand(id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/shopping-list")]
    [ProducesResponseType<ShoppingListHttpResponse>(StatusCodes.Status201Created)]
    [EnableIdempotency]
    public Task<IActionResult> GenerateShoppingList(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleCreated(userId.ToGenerateShoppingListCommand(id), static value => ShoppingListResponseMappings.ToHttpResponse(value));
}
