using System.Text;
using FoodDiary.Presentation.Api.Controllers;
using FoodDiary.Presentation.Api.Features.Billing.Mappings;
using FoodDiary.Presentation.Api.Responses;
using FoodDiary.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Webhooks;

[ApiController]
[AllowAnonymous]
[Route("api/v{version:apiVersion}/billing/webhooks/{provider}")]
public sealed class BillingWebhookController(ISender mediator) : BaseApiController(mediator) {
    private const string PaddleProvider = "Paddle";
    private const string YooKassaProvider = "YooKassa";

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesApiErrorResponse(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandleWebhook([FromRoute] string provider) {
        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        Request.Body.Position = 0;

        var signatureHeader = ResolveSignatureHeader(provider);
        return await HandleNoContent(provider.ToWebhookCommand(payload, signatureHeader));
    }

    private string ResolveSignatureHeader(string provider) {
        if (string.Equals(provider, PaddleProvider, StringComparison.OrdinalIgnoreCase)) {
            return Request.Headers["Paddle-Signature"].ToString();
        }

        if (string.Equals(provider, YooKassaProvider, StringComparison.OrdinalIgnoreCase)) {
            return string.Empty;
        }

        return Request.Headers["Stripe-Signature"].ToString();
    }
}
