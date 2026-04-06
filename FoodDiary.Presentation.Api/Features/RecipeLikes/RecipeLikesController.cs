using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.RecipeLikes.Mappings;
using FoodDiary.Presentation.Api.Features.RecipeLikes.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.RecipeLikes;

[ApiController]
[Route("api/v{version:apiVersion}/recipes/{recipeId:guid}/likes")]
public class RecipeLikesController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("toggle")]
    [ProducesResponseType<RecipeLikeStatusHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Toggle(
        [FromCurrentUser] Guid userId,
        Guid recipeId) =>
        HandleOk(RecipeLikeHttpMappings.ToCommand(userId, recipeId),
            static value => value.ToHttpResponse());

    [HttpGet]
    [ProducesResponseType<RecipeLikeStatusHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetStatus(
        [FromCurrentUser] Guid userId,
        Guid recipeId) =>
        HandleOk(RecipeLikeHttpMappings.ToQuery(userId, recipeId),
            static value => value.ToHttpResponse());
}
