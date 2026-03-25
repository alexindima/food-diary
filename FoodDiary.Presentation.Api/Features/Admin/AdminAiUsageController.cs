using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Extensions;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/admin/ai-usage")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminAiUsageController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("summary")]
    [ProducesResponseType<AdminAiUsageSummaryHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiErrorHttpResponse>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetSummary([FromQuery] GetAdminAiUsageSummaryHttpQuery query) {
        var result = await Mediator.Send(query.ToQuery());
        return result.ToOkActionResult(this, static value => value.ToHttpResponse());
    }
}
