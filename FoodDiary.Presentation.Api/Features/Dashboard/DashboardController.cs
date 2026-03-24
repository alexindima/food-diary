using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Dashboard.Mappings;
using FoodDiary.Presentation.Api.Features.Dashboard.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetDashboardSnapshotHttpQuery query) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }

    [HttpGet("advice")]
    public async Task<IActionResult> GetAdvice([FromQuery] GetDailyAdviceHttpQuery query) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }
}
