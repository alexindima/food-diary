using FoodDiary.Application.Fasting.Queries.GetFastingTelemetrySummary;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/telemetry")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminTelemetryController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("fasting")]
    [ProducesResponseType<FastingTelemetrySummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetFastingSummary([FromQuery] GetFastingTelemetrySummaryHttpQuery query) =>
        HandleOk(new GetFastingTelemetrySummaryQuery(query.Hours), static value => value.ToHttpResponse());
}
