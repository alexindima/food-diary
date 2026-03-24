using FoodDiary.Application.Recipes.Commands.DeleteRecipe;
using FoodDiary.Application.Recipes.Commands.DuplicateRecipe;
using FoodDiary.Application.Recipes.Mappings;
using FoodDiary.Application.Recipes.Queries.GetRecentRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipeById;
using FoodDiary.Application.Recipes.Queries.GetRecipes;
using FoodDiary.Application.Recipes.Queries.GetRecipesWithRecent;
using FoodDiary.Contracts.Recipes;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Recipes;

[ApiController]
[Route("api/[controller]")]
public class RecipesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool includePublic = true) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetRecipesQuery(userId, page, limit, search, includePublic);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("with-recent")]
    public async Task<IActionResult> GetAllWithRecent(
        [FromQuery] int page = 1,
        [FromQuery] int limit = 10,
        [FromQuery] int recentLimit = 10,
        [FromQuery] string? search = null,
        [FromQuery] bool includePublic = true) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var sanitizedPage = Math.Max(page, 1);
        var sanitizedLimit = Math.Clamp(limit, 1, 100);
        var sanitizedRecentLimit = Math.Clamp(recentLimit, 1, 50);
        var sanitizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();

        var query = new GetRecipesWithRecentQuery(
            userId,
            sanitizedPage,
            sanitizedLimit,
            sanitizedSearch,
            includePublic,
            sanitizedRecentLimit);

        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("recent")]
    public async Task<IActionResult> GetRecent(
        [FromQuery] int limit = 10,
        [FromQuery] bool includePublic = true) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var sanitizedLimit = Math.Clamp(limit, 1, 50);
        var query = new GetRecentRecipesQuery(userId, sanitizedLimit, includePublic);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] bool includePublic = true) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetRecipeByIdQuery(userId, new RecipeId(id), includePublic);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateRecipeRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPatch("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateRecipeRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId.Value, id);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new DeleteRecipeCommand(userId, new RecipeId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("{id:guid}/duplicate")]
    public async Task<IActionResult> Duplicate(Guid id) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new DuplicateRecipeCommand(userId, new RecipeId(id));
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
