using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Users;

[ApiController]
[Route("api/v{version:apiVersion}/users/waist-goals")]
public sealed class WaistGoalsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WaistGoalHistoryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetHistory([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToWaistGoalHistoryQuery(), static values => values.Select(static value => value.ToHttpResponse()).ToList());
}
