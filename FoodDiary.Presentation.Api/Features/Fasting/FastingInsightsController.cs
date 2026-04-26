using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Fasting.Mappings;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Fasting;

[ApiController]
[Route("api/v{version:apiVersion}/fasting")]
public sealed class FastingInsightsController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("stats")]
    [ProducesResponseType<FastingStatsHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetStats([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToStatsQuery(), static value => value.ToHttpResponse());

    [HttpGet("insights")]
    [ProducesResponseType<FastingInsightsHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetInsights([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToInsightsQuery(), static value => value.ToHttpResponse());
}
