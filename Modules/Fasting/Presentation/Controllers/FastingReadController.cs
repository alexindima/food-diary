using FoodDiary.Modules.Fasting.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Fasting.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Fasting.Presentation.Requests;
using FoodDiary.Modules.Fasting.Presentation.Contracts.Responses;
using FoodDiary.Modules.Fasting.Presentation.Responses;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Fasting.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/fasting")]
public sealed class FastingReadController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("current")]
    [ProducesResponseType<FastingSessionHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> GetCurrent([FromCurrentUser] Guid userId) =>
        HandleOptional(userId.ToCurrentQuery(), static value => value?.ToHttpResponse());

    [HttpGet("overview")]
    [ProducesResponseType<FastingOverviewHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetOverview([FromCurrentUser] Guid userId) =>
        HandleOk(userId.ToOverviewQuery(), static value => value.ToHttpResponse());

    [HttpGet("history")]
    [ProducesResponseType<PagedHttpResponse<FastingSessionHttpResponse>>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetHistory([FromCurrentUser] Guid userId, [FromQuery] GetFastingHistoryHttpQuery query) =>
        HandleOk(query.ToHistoryQuery(userId), static value => value.ToHttpResponse());
}
