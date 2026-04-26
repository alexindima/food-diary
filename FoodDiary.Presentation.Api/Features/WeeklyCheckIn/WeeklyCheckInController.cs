using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Mappings;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.WeeklyCheckIn;

[ApiController]
[Route("api/v{version:apiVersion}/weekly-check-in")]
public class WeeklyCheckInController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<WeeklyCheckInHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToQuery(), static value => value.ToHttpResponse());
}
