using System.ComponentModel.DataAnnotations;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Modules.Billing.Presentation.Controllers;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting(PresentationPolicyNames.WebhookRateLimitPolicyName)]
[Route("api/v{version:apiVersion}/billing/webhooks/{provider}")]
public sealed class BillingWebhookController(ISender mediator, BillingWebhookHttpProcessor processor) : BaseApiController(mediator) {
    [HttpPost]
    [RequestSizeLimit(BillingWebhookHttpProcessor.MaxWebhookPayloadBytes)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    [ProducesApiErrorResponse(StatusCodes.Status413PayloadTooLarge)]
    [ProducesApiErrorResponse(StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> HandleWebhook(
        [FromRoute, Required, MaxLength(BillingWebhookRequestLimits.MaximumProviderLength)] string provider) =>
        await HandleNoContent(await processor.CreateCommandAsync(Request, provider, HttpContext.RequestAborted));
}
