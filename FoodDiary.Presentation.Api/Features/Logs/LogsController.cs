using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Logs.Requests;
using FoodDiary.Presentation.Api.Filters;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Presentation.Api.Telemetry;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Logs;

[ApiController]
[AllowAnonymous]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/v{version:apiVersion}/logs")]
[SuppressRequestAccessLog]
[EnableRateLimiting(PresentationPolicyNames.ClientTelemetryRateLimitPolicyName)]
public sealed class LogsController(ISender sender, ClientTelemetryHttpProcessor processor) : BaseApiController(sender) {
    public const int MaxPayloadBytes = 64 * 1024;

    [HttpPost]
    [RequestSizeLimit(MaxPayloadBytes)]
    [RejectOversizedRequest(MaxPayloadBytes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(typeof(ApiErrorHttpResponse), StatusCodes.Status429TooManyRequests)]
    public Task<IActionResult> Create([FromBody] ClientTelemetryLogHttpRequest request) =>
        HandleNoContent(processor.ProcessAsync(request, HttpContext.RequestAborted));
}
