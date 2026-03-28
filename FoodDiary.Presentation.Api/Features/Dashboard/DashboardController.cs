using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Dashboard.Mappings;
using FoodDiary.Presentation.Api.Features.Dashboard.Requests;
using FoodDiary.Presentation.Api.Features.Dashboard.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace FoodDiary.Presentation.Api.Features.Dashboard;

[ApiController]
[Route("api/dashboard")]
public class DashboardController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [OutputCache(PolicyName = PresentationPolicyNames.UserScopedCachePolicyName)]
    [ProducesResponseType<DashboardSnapshotHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId, [FromQuery] GetDashboardSnapshotHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());

    [HttpGet("advice")]
    [ProducesResponseType<DailyAdviceHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> GetAdvice([FromCurrentUser] Guid userId, [FromQuery] GetDailyAdviceHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());
}

