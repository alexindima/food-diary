using System.Buffers;
using System.Net;
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
        EnsureTrustedYooKassaSource(request.HttpContext, provider);
        string payload = await ReadBoundedPayloadAsync(request, cancellationToken).ConfigureAwait(false);

        string signatureHeader = provider.ToUpperInvariant() switch {
            "PADDLE" => request.Headers["Paddle-Signature"].ToString(),
            "YOOKASSA" => string.Empty,
            _ => request.Headers["Stripe-Signature"].ToString(),
        };

        return provider.ToWebhookCommand(payload, signatureHeader);
    }

    public static bool IsTrustedYooKassaSource(IPAddress? address) {
        if (address is null) {
            return false;
        }

        IPAddress normalizedAddress = address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        return YooKassaSourceNetworks.Any(network => network.Contains(normalizedAddress));
    }

    private static void EnsureTrustedYooKassaSource(HttpContext context, string provider) {
        if (string.Equals(provider, "yookassa", StringComparison.OrdinalIgnoreCase) &&
            !IsTrustedYooKassaSource(context.Connection.RemoteIpAddress)) {
            throw new BadHttpRequestException("YooKassa webhook source is not trusted.", StatusCodes.Status403Forbidden);
        }
    }

    private static readonly IPNetwork[] YooKassaSourceNetworks = [
        new(IPAddress.Loopback, 32),
        new(IPAddress.IPv6Loopback, 128),
        new(IPAddress.Parse("185.71.76.0"), 27),
        new(IPAddress.Parse("185.71.77.0"), 27),
        new(IPAddress.Parse("77.75.153.0"), 25),
        new(IPAddress.Parse("77.75.156.11"), 32),
        new(IPAddress.Parse("77.75.156.35"), 32),
        new(IPAddress.Parse("77.75.154.128"), 25),
        new(IPAddress.Parse("2a02:5180::"), 32),
    ];

    private static async Task<string> ReadBoundedPayloadAsync(HttpRequest request, CancellationToken cancellationToken) {
        if (request.ContentLength is > MaxWebhookPayloadBytes) {
            throw new BadHttpRequestException("Billing webhook payload exceeds the allowed size.", StatusCodes.Status413PayloadTooLarge);
        }

        request.EnableBuffering();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
        try {
            var payload = new MemoryStream(
                request.ContentLength is > 0 ? (int)request.ContentLength.Value : 0);
            string payloadText;
            await using (payload.ConfigureAwait(false)) {
                int totalBytes = 0;
                while (true) {
                    int bytesRead = await request.Body.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                    if (bytesRead == 0) {
                        payloadText = Encoding.UTF8.GetString(payload.GetBuffer(), 0, totalBytes);
                        break;
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

            return payloadText;
        } finally {
            ArrayPool<byte>.Shared.Return(buffer);
            if (request.Body.CanSeek) {
                request.Body.Position = 0;
            }
        }
    }
}
