using System.Buffers;
using System.Text;
using FoodDiary.Application.Billing.Commands.ProcessBillingWebhook;
using FoodDiary.Presentation.Api.Features.Billing.Mappings;
using Microsoft.AspNetCore.Http;

namespace FoodDiary.Presentation.Api.Features.Billing;

public sealed class BillingWebhookHttpProcessor {
    public const int MaxWebhookPayloadBytes = 64 * 1024;

    public async Task<ProcessBillingWebhookCommand> CreateCommandAsync(
        HttpRequest request,
        string provider,
        CancellationToken cancellationToken) {
        string payload = await ReadBoundedPayloadAsync(request, cancellationToken).ConfigureAwait(false);

        string signatureHeader = provider.ToUpperInvariant() switch {
            "PADDLE" => request.Headers["Paddle-Signature"].ToString(),
            "YOOKASSA" => string.Empty,
            _ => request.Headers["Stripe-Signature"].ToString(),
        };

        return provider.ToWebhookCommand(payload, signatureHeader);
    }

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
    private static async Task<string> ReadBoundedPayloadAsync(HttpRequest request, CancellationToken cancellationToken) {
        if (request.ContentLength is > MaxWebhookPayloadBytes) {
            throw new BadHttpRequestException("Billing webhook payload exceeds the allowed size.", StatusCodes.Status413PayloadTooLarge);
        }

        request.EnableBuffering();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
        try {
            var payload = new MemoryStream(
                request.ContentLength is > 0 ? (int)request.ContentLength.Value : 0);
            await using (payload.ConfigureAwait(false)) {
                int totalBytes = 0;
                while (true) {
                    int bytesRead = await request.Body.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0) {
                        return Encoding.UTF8.GetString(payload.GetBuffer(), 0, totalBytes);
                    }

                    totalBytes += bytesRead;
                    if (totalBytes > MaxWebhookPayloadBytes) {
                        throw new BadHttpRequestException(
                            "Billing webhook payload exceeds the allowed size.",
                            StatusCodes.Status413PayloadTooLarge);
                    }

                    await payload.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken).ConfigureAwait(false);
                }
            }
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
            if (request.Body.CanSeek) {
                request.Body.Position = 0;
            }
        }
    }
}
