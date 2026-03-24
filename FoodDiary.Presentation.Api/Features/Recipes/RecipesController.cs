using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
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
        return result.ToActionResult();
    }

    [HttpGet("with-recent")]
    public async Task<IActionResult> GetAllWithRecent([FromCurrentUser] UserId userId, [FromQuery] GetRecipesWithRecentHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent([FromCurrentUser] UserId userId, [FromQuery] GetRecentRecipesHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromCurrentUser] UserId userId, [FromQuery] bool includePublic = true) {
        var query = new GetRecipeByIdQuery(userId, new RecipeId(id), includePublic);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromCurrentUser] UserId userId, [FromBody] CreateRecipeHttpRequest request) {
        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromCurrentUser] UserId userId, [FromBody] UpdateRecipeHttpRequest request) {
        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromCurrentUser] UserId userId) {
        var command = new DeleteRecipeCommand(userId, new RecipeId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id, [FromCurrentUser] UserId userId) {
        var command = new DuplicateRecipeCommand(userId, new RecipeId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
