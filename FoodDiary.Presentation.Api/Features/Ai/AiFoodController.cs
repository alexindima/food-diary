using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
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
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> AnalyzeFood([FromCurrentUser] Guid userId, [FromBody] FoodVisionHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost("nutrition")]
    [ProducesResponseType<FoodNutritionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status429TooManyRequests)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CalculateNutrition([FromCurrentUser] Guid userId, [FromBody] FoodNutritionHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}

