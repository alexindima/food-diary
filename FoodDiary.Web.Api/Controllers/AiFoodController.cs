using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Contracts.Ai;
using FoodDiary.Domain.Enums;
using FoodDiary.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/ai/food")]
[Authorize(Roles = RoleNames.Premium)]
public sealed class AiFoodController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpPost("vision")]
    public async Task<IActionResult> AnalyzeFood([FromBody] FoodVisionRequest request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var command = new AnalyzeFoodImageCommand(
            CurrentUserId.Value,
            new ImageAssetId(request.ImageAssetId),
            request.Description);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }

    [HttpPost("nutrition")]
    public async Task<IActionResult> CalculateNutrition([FromBody] FoodNutritionRequest request)
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var command = new CalculateFoodNutritionCommand(CurrentUserId.Value, request.Items ?? Array.Empty<FoodVisionItem>());
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
