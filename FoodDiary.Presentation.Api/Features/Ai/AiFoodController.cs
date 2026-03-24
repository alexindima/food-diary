using FoodDiary.Domain.Enums;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Ai.Mappings;
using FoodDiary.Presentation.Api.Features.Ai.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Ai;

[ApiController]
[Route("api/ai/food")]
[Authorize(Roles = RoleNames.Premium)]
public sealed class AiFoodController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("vision")]
    public async Task<IActionResult> AnalyzeFood([FromBody] FoodVisionHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("nutrition")]
    public async Task<IActionResult> CalculateNutrition([FromBody] FoodNutritionHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
