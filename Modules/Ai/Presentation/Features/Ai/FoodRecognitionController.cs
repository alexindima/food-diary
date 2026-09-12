using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Ai.Mappings;
using FoodDiary.Presentation.Api.Features.Ai.Requests;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Ai;

[ApiController]
[Route("api/v{version:apiVersion}/ai/food/recognitions")]
[Authorize]
public sealed class FoodRecognitionController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost]
    [Authorize(Roles = PresentationRoleNames.Premium)]
    [EnableRateLimiting(PresentationPolicyNames.AiRateLimitPolicyName)]
    [RequestSizeLimit(PresentationRequestLimits.AiPayloadBytes)]
    [RejectOversizedRequest(PresentationRequestLimits.AiPayloadBytes)]
    [ProducesResponseType<FoodRecognitionJobHttpResponse>(StatusCodes.Status202Accepted)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status409Conflict)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> Start([FromCurrentUser] Guid userId, [FromBody] StartFoodRecognitionHttpRequest request) =>
        HandleAccepted(request.ToCommand(userId), static job => job.ToHttpResponse());

    [HttpGet("{id:guid}")]
    [ProducesResponseType<FoodRecognitionJobHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId, Guid id) =>
        HandleOk(id.ToRecognitionQuery(userId), static job => job.ToHttpResponse());

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<FoodRecognitionJobHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> List([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToRecognitionListQuery(),
            static jobs => (IReadOnlyList<FoodRecognitionJobHttpResponse>)[.. jobs.Select(job => job.ToHttpResponse())]);
}
