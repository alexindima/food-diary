using FoodDiary.Modules.Users.Presentation.Goals.Mappings;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.Users.Presentation.Goals.Requests;
using FoodDiary.Modules.Users.Presentation.Goals.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Users.Presentation.Goals.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/goals")]
public sealed class GoalsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<GoalsHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetGoals([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToQuery(), static value => value.ToHttpResponse());

    [HttpPatch]
    [ProducesResponseType<GoalsHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> UpdateGoals([FromCurrentUser] Guid userId, [FromBody] UpdateGoalsHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());
}

