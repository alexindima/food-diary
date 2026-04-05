using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Ai.Mappings;
using FoodDiary.Presentation.Api.Features.Ai.Requests;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
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
    private readonly ILogger<AiFoodController> _logger = logger;

    [HttpPost("vision")]
    [ProducesResponseType<FoodVisionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> AnalyzeFood([FromCurrentUser] Guid userId, [FromBody] FoodVisionHttpRequest request) =>
        HandleObservedOk(request.ToCommand(userId), static value => value.ToHttpResponse(), _logger, "ai.food.vision", userId);

    [HttpPost("text")]
    [ProducesResponseType<FoodVisionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> ParseFoodText([FromCurrentUser] Guid userId, [FromBody] FoodTextHttpRequest request) =>
        HandleObservedOk(request.ToCommand(userId), static value => value.ToHttpResponse(), _logger, "ai.food.text", userId);

    [HttpPost("nutrition")]
    [ProducesResponseType<FoodNutritionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> CalculateNutrition([FromCurrentUser] Guid userId, [FromBody] FoodNutritionHttpRequest request) =>
        HandleObservedOk(request.ToCommand(userId), static value => value.ToHttpResponse(), _logger, "ai.food.nutrition", userId);
}

