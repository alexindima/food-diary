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
    public async Task<IActionResult> Get([FromCurrentUser] Guid userId, [FromQuery] GetDashboardSnapshotHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }

    [HttpGet("advice")]
    public async Task<IActionResult> GetAdvice([FromCurrentUser] Guid userId, [FromQuery] GetDailyAdviceHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
