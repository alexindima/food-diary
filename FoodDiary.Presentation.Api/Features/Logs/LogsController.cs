using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Presentation.Api.Features.Logs.Mappings;
using FoodDiary.Presentation.Api.Telemetry;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Features.Logs;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/v{version:apiVersion}/logs")]
[SuppressRequestAccessLog]
public sealed class LogsController(ISender sender, ClientTelemetryHttpProcessor processor) : BaseApiController(sender) {
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public Task<IActionResult> Create([FromBody] ClientTelemetryLogHttpRequest request) =>
        HandleNoContent(request.ToCommand(), result => processor.ProcessAsync(request, result, HttpContext.RequestAborted));
}
