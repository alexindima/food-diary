using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Fasting.Mappings;
using FoodDiary.Presentation.Api.Features.Fasting.Requests;
using FoodDiary.Presentation.Api.Features.Fasting.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Fasting;

[ApiController]
[Route("api/v{version:apiVersion}/fasting")]
public class FastingReadController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("current")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetCurrent([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToCurrentQuery(), static value => value?.ToHttpResponse());

    [HttpGet("overview")]
    [ProducesResponseType<FastingOverviewHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetOverview([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToOverviewQuery(), static value => value.ToHttpResponse());

    [HttpGet("history")]
    [ProducesResponseType<PagedHttpResponse<FastingSessionHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetHistory([FromCurrentUser] Guid userId, [FromQuery] GetFastingHistoryHttpQuery query) =>
        HandleOk(query.ToHistoryQuery(userId), static value => value.ToHttpResponse());
}
