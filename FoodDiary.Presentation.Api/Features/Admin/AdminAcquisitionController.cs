using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Admin.Mappings;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/acquisition")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminAcquisitionController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("summary")]
    [ProducesResponseType<MarketingAttributionSummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetSummary([FromQuery] GetMarketingAttributionSummaryHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());
}
