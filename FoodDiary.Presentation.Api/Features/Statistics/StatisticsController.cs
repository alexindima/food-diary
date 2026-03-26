using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Statistics.Mappings;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;
using FoodDiary.Presentation.Api.Features.Statistics.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Statistics;

[ApiController]
[Route("api/statistics")]
public class StatisticsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [ProducesResponseType<List<AggregatedStatisticsHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status500InternalServerError)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId, [FromQuery] GetStatisticsHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(item => item.ToHttpResponse()).ToList());
}

