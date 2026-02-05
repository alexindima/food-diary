using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FoodDiary.WebApi.Extensions;

namespace FoodDiary.WebApi.Controllers;

[ApiController]
[Route("api/admin/ai-usage")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminAiUsageController(ISender mediator) : BaseApiController(mediator)
{
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to)
    {
        var query = new GetAdminAiUsageSummaryQuery(from, to);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }
}
