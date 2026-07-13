using System.Text;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Presentation.Api.Features.Billing.Mappings;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Features.Billing;

public sealed class BillingWebhookHttpProcessor {
    public async Task<ProcessBillingWebhookCommand> CreateCommandAsync(
        HttpRequest request,
        string provider,
        CancellationToken cancellationToken) {
        request.EnableBuffering();
        using var reader = new StreamReader(request.Body, Encoding.UTF8, leaveOpen: true);
        string payload = await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
        request.Body.Position = 0;

        string signatureHeader = provider.ToUpperInvariant() switch {
            "PADDLE" => request.Headers["Paddle-Signature"].ToString(),
            "YOOKASSA" => string.Empty,
            _ => request.Headers["Stripe-Signature"].ToString(),
        };

        return provider.ToWebhookCommand(payload, signatureHeader);
    }
}
