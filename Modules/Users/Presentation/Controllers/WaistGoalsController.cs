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
[Route("api/v{version:apiVersion}/users/waist-goals")]
public sealed class WaistGoalsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WaistGoalHistoryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetHistory([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToWaistGoalHistoryQuery(), static values => values.Select(static value => value.ToHttpResponse()).ToList());

    [HttpGet("page")]
    [ProducesResponseType<WaistGoalHistoryPageHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetPage([FromCurrentUser] Guid userId, [FromQuery] string? cursor = null) =>
        HandleOk(userId.ToWaistGoalHistoryPageQuery(cursor), static page => new WaistGoalHistoryPageHttpResponse(
            page.Items.Select(static value => value.ToHttpResponse()).ToList(), page.NextCursor));
}
