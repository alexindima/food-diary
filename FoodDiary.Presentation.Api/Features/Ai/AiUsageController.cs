using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Ai.Mappings;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Ai;

[ApiController]
[Route("api/ai/usage")]
[Authorize]
public sealed class AiUsageController(ISender mediator) : AuthorizedController(mediator) {
    [HttpGet("me")]
    public async Task<IActionResult> GetMyUsage([FromCurrentUser] UserId userId) {
        var query = new GetUserAiUsageSummaryQuery(userId);
        var result = await Mediator.Send(query);
        return result.IsSuccess
            ? Ok(result.Value.ToHttpResponse())
            : result.ToActionResult();
    }
}
