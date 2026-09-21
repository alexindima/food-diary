using FoodDiary.Modules.Users.Application.Queries.GetWeightGoalHistoryPage;
using FoodDiary.Modules.Users.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Users.Presentation.Mappings;
using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.Users.Presentation.Contracts.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Users.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/users/weight-goals")]
public sealed class WeightGoalsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WeightGoalHistoryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetHistory([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToWeightGoalHistoryQuery(), static values => values.Select(static value => value.ToHttpResponse()).ToList());

    [HttpGet("page")]
    [ProducesResponseType<WeightGoalHistoryPageHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetPage([FromCurrentUser] Guid userId, [FromQuery] string? cursor = null) =>
        HandleOk(new GetWeightGoalHistoryPageQuery(userId, cursor), static page => new WeightGoalHistoryPageHttpResponse(
            page.Items.Select(static value => value.ToHttpResponse()).ToList(), page.NextCursor));
}
