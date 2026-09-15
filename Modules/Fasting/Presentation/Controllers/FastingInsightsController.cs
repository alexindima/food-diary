using FoodDiary.Modules.Fasting.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Fasting.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Fasting.Presentation.Controllers;

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
