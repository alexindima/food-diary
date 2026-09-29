using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeComments.Mappings;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeComments.Requests;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeComments.Responses;
using FoodDiary.Presentation.Api.Requests;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.RecipeCommunity.Presentation.RecipeComments.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/recipes/{recipeId:guid}/comments")]
public sealed class RecipeCommentsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<RecipeCommentHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll(
        [FromCurrentUser] Guid userId,
        Guid recipeId,
        [FromQuery] OffsetPaginationHttpQuery pagination) =>
        HandleOk(RecipeCommentHttpMappings.ToQuery(userId, recipeId, pagination.Page, pagination.Limit),
            static value => value.ToHttpResponse());

    [HttpPost]
    [EnableIdempotency]
    [ProducesResponseType<RecipeCommentHttpResponse>(StatusCodes.Status201Created)]
    public Task<IActionResult> Create(
        [FromCurrentUser] Guid userId,
        Guid recipeId,
        [FromBody] CreateRecipeCommentHttpRequest request) =>
        HandleCreated(request.ToCommand(userId, recipeId), static value => value.ToHttpResponse());

    [HttpPatch("{commentId:guid}")]
    [ProducesResponseType<RecipeCommentHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Update(
        [FromCurrentUser] Guid userId,
        Guid recipeId,
        Guid commentId,
        [FromBody] UpdateRecipeCommentHttpRequest request) =>
        HandleOk(request.ToCommand(userId, recipeId, commentId), static value => value.ToHttpResponse());

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(
        [FromCurrentUser] Guid userId,
        Guid recipeId,
        Guid commentId) =>
        HandleNoContent(RecipeCommentHttpMappings.ToDeleteCommand(userId, recipeId, commentId));
}
