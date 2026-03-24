using FoodDiary.Presentation.Api.Authorization;
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
[Authorize(Roles = PresentationRoleNames.Premium)]
public sealed class AiFoodController(ISender mediator) : AuthorizedController(mediator) {
    [HttpPost("vision")]
    public async Task<IActionResult> AnalyzeFood([FromCurrentUser] Guid userId, [FromBody] FoodVisionHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpPost("nutrition")]
    public async Task<IActionResult> CalculateNutrition([FromCurrentUser] Guid userId, [FromBody] FoodNutritionHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
