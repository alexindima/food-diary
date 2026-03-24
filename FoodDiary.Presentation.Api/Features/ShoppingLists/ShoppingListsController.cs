using FoodDiary.Application.ShoppingLists.Commands.DeleteShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetCurrentShoppingList;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingListById;
using FoodDiary.Application.ShoppingLists.Queries.GetShoppingLists;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Mappings;
using FoodDiary.Presentation.Api.Features.ShoppingLists.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.ShoppingLists;

[ApiController]
[Route("api/shopping-lists")]
public class ShoppingListsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent([FromCurrentUser] UserId userId) {
        var query = new GetCurrentShoppingListQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromCurrentUser] UserId userId) {
        var query = new GetShoppingListsQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromCurrentUser] UserId userId) {
        var query = new GetShoppingListByIdQuery(userId, new ShoppingListId(id));
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateShoppingListHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] UserId userId, [FromBody] UpdateShoppingListHttpRequest request) {
        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] UserId userId) {
        var command = new DeleteShoppingListCommand(userId, new ShoppingListId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
