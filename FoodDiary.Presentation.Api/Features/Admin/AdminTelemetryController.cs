using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/telemetry")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminTelemetryController(IFastingTelemetrySummaryService fastingTelemetrySummaryService) : ControllerBase {
    [HttpGet("fasting")]
    [ProducesResponseType<FastingTelemetrySummaryHttpResponse>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFastingSummary([FromQuery] GetFastingTelemetrySummaryHttpQuery query) {
        var response = (await fastingTelemetrySummaryService.GetSummaryAsync(query.Hours, HttpContext.RequestAborted)).ToHttpResponse();
        return new OkObjectResult(response);
    }
}
