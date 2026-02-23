using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Web.Api.Controllers;
using FoodDiary.Web.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Web.Api.Features.Ai;

[ApiController]
[Route("api/ai/usage")]
[Authorize]
public sealed class AiUsageController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("me")]
    public async Task<IActionResult> GetMyUsage() {
        if (!TryGetCurrentUserId(out var userId)) {
            return Unauthorized();
        }

        var query = new GetUserAiUsageSummaryQuery(userId);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }
}
