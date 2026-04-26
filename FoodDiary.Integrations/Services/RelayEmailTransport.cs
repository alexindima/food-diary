using System.Net.Mail;
using System.Net.Mime;
using System.Text;
using FoodDiary.Application.Email.Common;
using FoodDiary.MailRelay.Client;
using FoodDiary.MailRelay.Client.Models;

namespace FoodDiary.Integrations.Services;

internal sealed class RelayEmailTransport(IMailRelayClient mailRelayClient) : IEmailTransport {
    public async Task SendAsync(MailMessage message, CancellationToken cancellationToken) {
        await mailRelayClient.EnqueueAsync(CreatePayload(message), cancellationToken);
    }

    private static EnqueueMailRelayEmailRequest CreatePayload(MailMessage message) {
        if (message.From is null) {
            throw new InvalidOperationException("Email message must include a From address.");
        }

        return new EnqueueMailRelayEmailRequest(
            message.From.Address,
            message.From.DisplayName,
            message.To.Select(static recipient => recipient.Address).ToArray(),
            message.Subject,
            message.Body,
            GetPlainTextBody(message.AlternateViews),
            CorrelationId: Guid.NewGuid().ToString("N"));
    }

    private static string? GetPlainTextBody(AlternateViewCollection alternateViews) {
        foreach (var view in alternateViews) {
            if (!string.Equals(view.ContentType.MediaType, MediaTypeNames.Text.Plain, StringComparison.OrdinalIgnoreCase)) {
                continue;
            }

            if (view.ContentStream.CanSeek) {
                view.ContentStream.Position = 0;
            }

            using var reader = new StreamReader(view.ContentStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            var content = reader.ReadToEnd();
            if (view.ContentStream.CanSeek) {
                view.ContentStream.Position = 0;
            }

            return content;
        }

        return null;
    }
}
