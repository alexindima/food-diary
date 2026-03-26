using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Policies;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/admin/ai-usage")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminAiUsageController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("summary")]
    [ProducesResponseType<AdminAiUsageSummaryHttpResponse>(StatusCodes.Status200OK)]
    [OutputCache(PolicyName = PresentationPolicyNames.AdminAiUsageCachePolicyName)]
    public Task<IActionResult> GetSummary([FromQuery] GetAdminAiUsageSummaryHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());
}
