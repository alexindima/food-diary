using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Modules.Dashboard.Presentation.Mappings;
using FoodDiary.Modules.Dashboard.Presentation.Mappings.Mappings;
using FoodDiary.Modules.Dashboard.Presentation.Requests;
using FoodDiary.Modules.Dashboard.Presentation.Contracts.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Dashboard.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController(ISender mediator) : AuthorizedController(mediator) {
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

    [HttpPost("test-email")]
    [EnableIdempotency]
    [EnableRateLimiting(PresentationPolicyNames.TestDeliveryRateLimitPolicyName)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> SendTestEmail([FromCurrentUser] Guid userId) =>
        HandleNoContent(userId.ToTestEmailCommand());
}
