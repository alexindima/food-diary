using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/ai-usage")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminAiUsageController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("summary")]
    [ProducesResponseType<AdminAiUsageSummaryHttpResponse>(StatusCodes.Status200OK)]
    [OutputCache(PolicyName = PresentationPolicyNames.AdminAiUsageCachePolicyName)]
    public Task<IActionResult> GetSummary([FromQuery] GetAdminAiUsageSummaryHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());
}
