using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Domain.ValueObjects.Ids;
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
    public async Task<IActionResult> GetGoals([FromCurrentUser] UserId userId) {
        var query = new GetUserGoalsQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateGoals([FromCurrentUser] UserId userId, [FromBody] UpdateGoalsHttpRequest request) {
        var command = request.ToCommand(userId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
