using FoodDiary.Domain.ValueObjects.Ids;
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
    public async Task<IActionResult> Get([FromCurrentUser] UserId userId, [FromQuery] GetStatisticsHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery(userId));
        return result.ToActionResult();
    }
}
