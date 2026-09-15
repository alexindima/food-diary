using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Mappings;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Requests;
using FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.RecipeCommunity.Presentation.RecipeLikes.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/recipes/{recipeId:guid}/likes")]
public sealed class RecipeLikesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("toggle")]
    [EnableIdempotency(requireKey: true)]
    [ProducesResponseType<RecipeLikeStatusHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Toggle(
        [FromCurrentUser] Guid userId,
        Guid recipeId,
        [FromBody] SetRecipeLikeStateHttpRequest request) =>
        HandleOk(RecipeLikeHttpMappings.ToCommand(userId, recipeId, request.IsLiked!.Value),
            static value => value.ToHttpResponse());

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType<RecipeLikeStatusHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetStatus(
        [FromCurrentUser] Guid userId,
        Guid recipeId) =>
        HandleOk(RecipeLikeHttpMappings.ToQuery(userId, recipeId),
            static value => value.ToHttpResponse());
}
