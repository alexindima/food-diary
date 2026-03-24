using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Domain.Enums;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/admin/ai-usage")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminAiUsageController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] DateOnly? from, [FromQuery] DateOnly? to) {
        var query = new GetAdminAiUsageSummaryQuery(from, to);
        var result = await Mediator.Send(query);
        return result.ToActionResult();
    }
}
