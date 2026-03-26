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

namespace FoodDiary.Presentation.Api.Features.Ai;

[ApiController]
[Route("api/ai/food")]
[Authorize(Roles = PresentationRoleNames.Premium)]
[EnableRateLimiting(PresentationPolicyNames.AiRateLimitPolicyName)]
public sealed class AiFoodController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("vision")]
    [ProducesResponseType<FoodVisionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> AnalyzeFood([FromCurrentUser] Guid userId, [FromBody] FoodVisionHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());

    [HttpPost("nutrition")]
    [ProducesResponseType<FoodNutritionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public Task<IActionResult> CalculateNutrition([FromCurrentUser] Guid userId, [FromBody] FoodNutritionHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());
}

