using FoodDiary.Application.Abstractions.Email.Common;
using FoodDiary.MailRelay.Client;
using FoodDiary.MailRelay.Client.Models;

namespace FoodDiary.Integrations.Services;

internal sealed class RelayEmailTransport(IMailRelayClient mailRelayClient) : IEmailTransport {
    public async Task SendAsync(EmailMessage message, CancellationToken cancellationToken) {
        await mailRelayClient.EnqueueAsync(CreatePayload(message), cancellationToken).ConfigureAwait(false);
    }

    private static EnqueueMailRelayEmailRequest CreatePayload(EmailMessage message) {
        if (string.IsNullOrWhiteSpace(message.FromAddress)) {
            throw new InvalidOperationException("Email message must include a From address.");
        }

        return new EnqueueMailRelayEmailRequest(
            message.FromAddress,
            message.FromName,
            [.. message.ToAddresses],
            message.Subject,
            message.HtmlBody,
            message.TextBody,
            CorrelationId: Guid.NewGuid().ToString("N"));
    }
}
