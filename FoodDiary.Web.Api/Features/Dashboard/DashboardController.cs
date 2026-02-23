using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Web.Api.Controllers;
using FoodDiary.Web.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Web.Api.Features.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime date,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string locale = "en",
        [FromQuery] int trendDays = 7) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetDashboardSnapshotQuery(userId, date, page, pageSize, locale, trendDays);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }

    [HttpGet("advice")]
    public async Task<IActionResult> GetAdvice(
        [FromQuery] DateTime date,
        [FromQuery] string locale = "en") {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetDailyAdviceQuery(userId, date, locale);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }
}
