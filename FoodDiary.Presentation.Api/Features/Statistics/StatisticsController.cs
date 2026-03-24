using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Statistics;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime dateFrom,
        [FromQuery] DateTime dateTo,
        [FromQuery] int quantizationDays = 1) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetStatisticsQuery(userId, dateFrom, dateTo, quantizationDays);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }
}
