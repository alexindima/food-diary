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
    public async Task<IActionResult> GetCurrent([FromCurrentUser] Guid userId) {
        var result = await Mediator.Send(userId.ToCurrentQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromCurrentUser] Guid userId) {
        var result = await Mediator.Send(userId.ToListQuery());
        return result.ToOkActionResult(this, static value => value.Select(x => x.ToHttpResponse()).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromCurrentUser] Guid userId) {
        var result = await Mediator.Send(id.ToGetByIdQuery(userId));
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] Guid userId, [FromBody] CreateShoppingListHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] Guid userId, [FromBody] UpdateShoppingListHttpRequest request) {
        var command = request.ToCommand(userId, id);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] Guid userId) {
        var command = id.ToDeleteCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToNoContentActionResult();
    }
}
