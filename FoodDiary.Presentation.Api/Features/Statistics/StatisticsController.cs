using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Statistics.Mappings;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;
using FoodDiary.Presentation.Api.Features.Statistics.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace FoodDiary.Presentation.Api.Features.Statistics;

[ApiController]
[Route("api/v{version:apiVersion}/statistics")]
public class StatisticsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [OutputCache(PolicyName = PresentationPolicyNames.UserScopedCachePolicyName)]
    [ProducesResponseType<List<AggregatedStatisticsHttpResponse>>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId, [FromQuery] GetStatisticsHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.Select(item => item.ToHttpResponse()).ToList());
}

