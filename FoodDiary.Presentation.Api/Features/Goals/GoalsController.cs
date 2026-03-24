using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Goals.Mappings;
using FoodDiary.Presentation.Api.Features.Goals.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Goals;

[ApiController]
[Route("api/[controller]")]
public class GoalsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> GetGoals() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetUserGoalsQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateGoals([FromBody] UpdateGoalsHttpRequest request) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
