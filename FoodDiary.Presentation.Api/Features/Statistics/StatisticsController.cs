using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Statistics.Mappings;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Statistics;

[ApiController]
[Route("api/[controller]")]
public class StatisticsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] GetStatisticsHttpQuery query) {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }
}
