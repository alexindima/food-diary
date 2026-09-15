using FoodDiary.Modules.WeeklyCheckIn.Presentation.Mappings;
using FoodDiary.Presentation.Api.Controllers;

using FoodDiary.Modules.WeeklyCheckIn.Presentation.Requests;
using FoodDiary.Modules.WeeklyCheckIn.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.WeeklyCheckIn.Presentation.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/weekly-check-in")]
public sealed class WeeklyCheckInController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet]
    [EnableRateLimiting(PresentationPolicyNames.FoodDataRateLimitPolicyName)]
    [OutputCache(PolicyName = PresentationPolicyNames.UserScopedCachePolicyName)]
    [ProducesResponseType<WeeklyCheckInHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> Get([FromCurrentUser] Guid userId, [FromQuery] GetWeeklyCheckInHttpQuery query) =>
        HandleOk(query.ToQuery(userId), static value => value.ToHttpResponse());
}
