using FoodDiary.Modules.WeeklyGoals.Presentation.Mappings;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.WeeklyGoals.Presentation.Requests;
using FoodDiary.Modules.WeeklyGoals.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.WeeklyGoals.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/weekly-goals")]
public sealed class WeeklyGoalsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<WeeklyGoalHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId, [FromQuery] GetWeeklyGoalHttpQuery query) =>
        HandleOptional(query.ToQuery(userId), static value => value?.ToHttpResponse());

    [HttpPut]
    [ProducesResponseType<WeeklyGoalHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Upsert([FromCurrentUser] Guid userId, [FromBody] UpsertWeeklyGoalHttpRequest request) =>
        HandleOk(request.ToCommand(userId), static value => value.ToHttpResponse());
}
