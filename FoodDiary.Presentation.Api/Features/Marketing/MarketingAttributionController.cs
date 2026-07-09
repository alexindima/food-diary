using FoodDiary.Mediator;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Marketing.Mappings;
using FoodDiary.Presentation.Api.Features.Marketing.Requests;
using FoodDiary.Presentation.Api.Telemetry;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Marketing;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/v{version:apiVersion}/marketing/attribution-events")]
[SuppressRequestAccessLog]
public sealed class MarketingAttributionController(ISender mediator) : BaseApiController(mediator) {
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Create([FromBody] MarketingAttributionHttpRequest request) =>
        HandleNoContent(request.ToCommand());
}
