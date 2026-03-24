using FoodDiary.Domain.Enums;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/admin/ai-usage")]
[Authorize(Roles = RoleNames.Admin)]
public sealed class AdminAiUsageController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary([FromQuery] GetAdminAiUsageSummaryHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
