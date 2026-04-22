using System.Text;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Billing.Mappings;
using FoodDiary.Presentation.Api.Responses;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Webhooks;

[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/billing/webhooks/{provider}")]
public sealed class BillingWebhookController(ISender mediator) : BaseApiController(mediator) {
    private const string PaddleProvider = "Paddle";

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook([FromRoute] string provider) {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var signatureHeader = string.Equals(provider, PaddleProvider, StringComparison.OrdinalIgnoreCase)
            ? Request.Headers["Paddle-Signature"].ToString()
            : Request.Headers["Stripe-Signature"].ToString();
        return await HandleNoContent(provider.ToWebhookCommand(payload, signatureHeader));
    }
}
