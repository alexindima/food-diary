using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dietologist.Mappings;
using FoodDiary.Presentation.Api.Features.Dietologist.Requests;
using FoodDiary.Presentation.Api.Features.Dietologist.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dietologist;

[ApiController]
[Authorize(Roles = PresentationRoleNames.Dietologist)]
[Route("api/v{version:apiVersion}/dietologist/recommendations/bulk")]
public sealed class BulkRecommendationsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost]
    [ProducesResponseType<BulkRecommendationResultHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Create(
        [FromCurrentUser] Guid userId,
        [FromBody] BulkCreateRecommendationsHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());
}
