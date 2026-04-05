using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.MealPlans.Mappings;
using FoodDiary.Presentation.Api.Features.MealPlans.Responses;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Responses;
using ShoppingListResponseMappings = FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings.ShoppingListHttpResponseMappings;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.MealPlans;

[ApiController]
[Route("api/v{version:apiVersion}/meal-plans")]
public class MealPlansController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<MealPlanSummaryHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll(
        [FromCurrentUser] Guid userId,
        [FromQuery] string? dietType = null) =>
        HandleOk(userId.ToQuery(dietType), static value => value.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetById(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleOk(userId.ToGetByIdQuery(id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/adopt")]
    [ProducesResponseType<MealPlanHttpResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Adopt(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleCreated(userId.ToAdoptCommand(id), static value => value.ToHttpResponse());

    [HttpPost("{id:guid}/shopping-list")]
    [ProducesResponseType<ShoppingListHttpResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> GenerateShoppingList(
        [FromCurrentUser] Guid userId,
        Guid id) =>
        HandleCreated(userId.ToGenerateShoppingListCommand(id), static value => ShoppingListResponseMappings.ToHttpResponse(value));
}
