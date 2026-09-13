using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Authorization;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin;

[ApiController]
[Route("api/v{version:apiVersion}/admin/acquisition")]
[Authorize(Roles = PresentationRoleNames.Admin)]
public sealed class AdminAcquisitionController(ISender mediator) : BaseApiController(mediator) {
    [HttpGet("range")]
    [ProducesResponseType<MarketingAttributionRangeHttpResponse>(StatusCodes.Status200OK)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> GetRange([FromQuery] GetMarketingAttributionRangeHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());

    [HttpGet("summary")]
    [ProducesResponseType<MarketingAttributionSummaryHttpResponse>(StatusCodes.Status200OK)]
    public Task<IActionResult> GetSummary([FromQuery] GetMarketingAttributionSummaryHttpQuery query) =>
        HandleOk(query.ToQuery(), static value => value.ToHttpResponse());
}
