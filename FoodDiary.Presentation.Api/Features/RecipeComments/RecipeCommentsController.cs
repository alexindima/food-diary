using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.RecipeComments.Mappings;
using FoodDiary.Presentation.Api.Features.RecipeComments.Requests;
using FoodDiary.Presentation.Api.Features.RecipeComments.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.RecipeComments;

[ApiController]
[Route("api/v{version:apiVersion}/recipes/{recipeId:guid}/comments")]
public class RecipeCommentsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<PagedHttpResponse<RecipeCommentHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetAll(
        [FromCurrentUser] Guid userId,
        Guid recipeId,
        [FromQuery] int page = 1,
        [FromQuery] int limit = 20) =>
        HandleOk(RecipeCommentHttpMappings.ToQuery(userId, recipeId, page, limit),
            static value => value.ToHttpResponse());

    [HttpPost]
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
        HandleOk(request.ToCommand(userId, commentId), static value => value.ToHttpResponse());

    [HttpDelete("{commentId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Delete(
        [FromCurrentUser] Guid userId,
        Guid recipeId,
        Guid commentId) =>
        HandleNoContent(RecipeCommentHttpMappings.ToDeleteCommand(userId, recipeId, commentId));
}
