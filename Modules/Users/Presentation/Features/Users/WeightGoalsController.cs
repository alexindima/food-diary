using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Users.Mappings;
using FoodDiary.Presentation.Api.Features.Users.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Users;

[ApiController]
[Route("api/v{version:apiVersion}/users/weight-goals")]
public sealed class WeightGoalsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WeightGoalHistoryHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status404NotFound)]
    public Task<IActionResult> GetHistory([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToWeightGoalHistoryQuery(), static values => values.Select(static value => value.ToHttpResponse()).ToList());
}
