using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

[ApiController]
[Route("api/v{version:apiVersion}/recommendations")]
public class RecommendationsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<RecommendationHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetMyRecommendations([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToMyRecommendationsQuery(), static value => value.Select(x => x.ToHttpResponse()).ToList());

    [HttpPut("{recommendationId:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> MarkAsRead(Guid recommendationId, [FromCurrentUser] Guid userId) =>
        HandleNoContent(recommendationId.ToMarkReadCommand(userId));
}
