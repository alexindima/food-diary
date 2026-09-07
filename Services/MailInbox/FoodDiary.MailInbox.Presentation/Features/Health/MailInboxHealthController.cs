using FoodDiary.MailInbox.Presentation.Controllers;
using FoodDiary.MailInbox.Presentation.Features.Health.Mappings;
using FoodDiary.MailInbox.Presentation.Features.Health.Responses;
using FoodDiary.MailInbox.Presentation.Filters;
using FoodDiary.MailInbox.Presentation.Options;
using FoodDiary.MailInbox.Presentation.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FoodDiary.MailInbox.Presentation.Features.Health;

[Route("")]
public sealed class MailInboxHealthController(
    ISender sender,
    IOptions<MailInboxHttpOptions> options) : MailInboxControllerBase(sender) {
    [HttpGet("health")]
    [ProducesResponseType<MailInboxHealthHttpResponse>(StatusCodes.Status200OK)]
    public IActionResult GetHealth() => Ok(MailInboxHealthHttpMappings.ToHealthHttpResponse());

    [HttpGet("health/ready")]
    [ServiceFilter(typeof(MailInboxReadinessConcurrencyFilter))]
    [ProducesResponseType<MailInboxHealthHttpResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<MailInboxApiErrorHttpResponse>(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetReady() {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(HttpContext.RequestAborted);
        timeoutSource.CancelAfter(options.Value.ReadinessExecutionTimeout);

        try {
            return await HandleOk(
                MailInboxHealthHttpMappings.ToReadinessQuery(),
                MailInboxHealthHttpMappings.ToReadyHttpResponse(),
                timeoutSource.Token);
        } catch (OperationCanceledException) when (!HttpContext.RequestAborted.IsCancellationRequested) {
            return StatusCode(
                StatusCodes.Status503ServiceUnavailable,
                new MailInboxApiErrorHttpResponse(
                    "MailInbox.ReadinessTimedOut",
                    "Mail inbox readiness verification timed out.",
                    HttpContext.TraceIdentifier));
        }
    }
}
