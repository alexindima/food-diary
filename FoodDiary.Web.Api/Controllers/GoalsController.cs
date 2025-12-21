using MediatR;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.Application.Users.Mappings;
using FoodDiary.Application.Users.Queries.GetUserGoals;
using FoodDiary.Application.Users.Commands.UpdateGoals;
using FoodDiary.Contracts.Goals;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GoalsController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet]
    public async Task<IActionResult> GetGoals()
    {
        var query = new GetUserGoalsQuery(CurrentUserId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpPatch]
    public async Task<IActionResult> UpdateGoals([FromBody] UpdateGoalsRequest request)
    {
        var command = request.ToCommand(CurrentUserId);
        var result = await Mediator.Send(command);
        return result.ToActionResult();
    }
}
