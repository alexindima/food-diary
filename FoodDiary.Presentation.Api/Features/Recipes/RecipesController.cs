using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Recipes.Mappings;
using FoodDiary.Presentation.Api.Features.Recipes.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Recipes;

[ApiController]
[Route("api/[controller]")]
public class RecipesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll([FromCurrentUser] UserId userId, [FromQuery] GetRecipesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpGet("with-recent")]
    public async Task<IActionResult> GetAllWithRecent([FromCurrentUser] UserId userId, [FromQuery] GetRecipesWithRecentHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromCurrentUser] UserId userId, [FromQuery] GetRecentRecipesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.Select(x => x.ToHttpResponse()).ToList());
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromCurrentUser] UserId userId, [FromQuery] bool includePublic = true) {
        var result = await Mediator.Send(id.ToQuery(userId, includePublic));
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateRecipeHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] UserId userId, [FromBody] UpdateRecipeHttpRequest request) {
        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] UserId userId) {
        var command = id.ToDeleteCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToNoContentActionResult();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, [FromCurrentUser] UserId userId) {
        var command = id.ToDuplicateCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
