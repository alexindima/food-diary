using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Ai.Mappings;
using FoodDiary.Presentation.Api.Features.Ai.Requests;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace FoodDiary.Presentation.Api.Features.Ai;

[ApiController]
[Route("api/v{version:apiVersion}/ai/food")]
[Authorize(Roles = PresentationRoleNames.Premium)]
[EnableRateLimiting(PresentationPolicyNames.AiRateLimitPolicyName)]
public sealed class AiFoodController(ISender mediator, ILogger<AiFoodController> logger) : AuthorizedController(mediator) {
    [HttpPost("vision")]
    [EnableIdempotency(requireKey: true)]
    [ProducesResponseType<FoodVisionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> AnalyzeFood([FromCurrentUser] Guid userId, [FromBody] FoodVisionHttpRequest request) =>
        HandleObservedOk(request.ToCommand(userId, GetRequestId()), static value => value.ToHttpResponse(), logger, "ai.food.vision", userId);

    [HttpPost("text")]
    [EnableIdempotency(requireKey: true)]
    [ProducesResponseType<FoodVisionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> ParseFoodText([FromCurrentUser] Guid userId, [FromBody] FoodTextHttpRequest request) =>
        HandleObservedOk(request.ToCommand(userId, GetRequestId()), static value => value.ToHttpResponse(), logger, "ai.food.text", userId);

    [HttpPost("nutrition")]
    [EnableIdempotency(requireKey: true)]
    [ProducesResponseType<FoodNutritionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> CalculateNutrition([FromCurrentUser] Guid userId, [FromBody] FoodNutritionHttpRequest request) =>
        HandleObservedOk(request.ToCommand(userId, GetRequestId()), static value => value.ToHttpResponse(), logger, "ai.food.nutrition", userId);

    private string GetRequestId() =>
        IdempotencyRequestContext.GetRequestId(HttpContext) ?? throw new InvalidOperationException("Required idempotency context is unavailable.");
}
