using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Domain.ValueObjects;
using FoodDiary.WebApi.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/ai/usage")]
[Authorize]
public sealed class AiUsageController(ISender mediator) : AuthorizedController(mediator)
{
    [HttpGet("me")]
    public async Task<IActionResult> GetMyUsage()
    {
        if (CurrentUserId is null)
        {
            return Unauthorized();
        }

        var query = new GetUserAiUsageSummaryQuery(CurrentUserId.Value);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }
}
