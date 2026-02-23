using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Contracts.Ai;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Web.Api.Controllers;
using FoodDiary.Web.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Web.Api.Features.Ai;

[ApiController]
[Route("api/ai/food")]
[Authorize(Roles = RoleNames.Premium)]
public sealed class AiFoodController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("vision")]
    public async Task<IActionResult> AnalyzeFood([FromBody] FoodVisionRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new AnalyzeFoodImageCommand(
            userId,
            new ImageAssetId(request.ImageAssetId),
            request.Description);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("nutrition")]
    public async Task<IActionResult> CalculateNutrition([FromBody] FoodNutritionRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = new CalculateFoodNutritionCommand(userId, request.Items ?? Array.Empty<FoodVisionItem>());
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
