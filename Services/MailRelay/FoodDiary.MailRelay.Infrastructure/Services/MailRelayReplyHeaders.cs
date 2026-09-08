using MimeKit;

namespace FoodDiary.MailRelay.Infrastructure.Services;

internal static class MailRelayReplyHeaders {
    public static void Apply(MimeMessage message, RelayEmailMessageRequest request) {
        if (!string.IsNullOrWhiteSpace(request.ReplyTo)) {
            message.ReplyTo.Add(MailboxAddress.Parse(request.ReplyTo));
        }
        if (!string.IsNullOrWhiteSpace(request.InReplyTo)) {
            message.InReplyTo = request.InReplyTo;
            message.References.Add(request.InReplyTo);
        }
        if (request.AutoSubmitted) {
            message.Headers.Add("Auto-Submitted", "auto-replied");
            message.Headers.Add("X-Auto-Response-Suppress", "All");
        }
    }
}
