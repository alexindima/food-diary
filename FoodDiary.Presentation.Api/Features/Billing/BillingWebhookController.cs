using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Policies;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FoodDiary.Presentation.Api.Features.Billing;

[ApiController]
[AllowAnonymous]
[EnableRateLimiting(PresentationPolicyNames.AuthRateLimitPolicyName)]
[Route("api/v{version:apiVersion}/billing/webhooks/{provider}")]
public sealed class BillingWebhookController(ISender mediator, BillingWebhookHttpProcessor processor) : BaseApiController(mediator) {
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook([FromRoute] string provider) =>
        await HandleNoContent(await processor.CreateCommandAsync(Request, provider, HttpContext.RequestAborted));
}
