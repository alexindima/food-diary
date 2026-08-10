using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Mappings;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Requests;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.WeeklyCheckIn;

[ApiController]
[Route("api/v{version:apiVersion}/weekly-check-in")]
public sealed class WeeklyCheckInController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<WeeklyCheckInHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId, [FromQuery] GetWeeklyCheckInHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());
}
