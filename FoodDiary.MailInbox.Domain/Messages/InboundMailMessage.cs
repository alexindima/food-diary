using FoodDiary.MailInbox.Domain.Common;
using FoodDiary.MailInbox.Domain.Events;

namespace FoodDiary.MailInbox.Domain.Messages;

public sealed class InboundMailMessage : AggregateRoot<InboundMailMessageId> {
    private readonly List<string> _toRecipients = new();

    private InboundMailMessage() {
    }

    private InboundMailMessage(InboundMailMessageId id) : base(id) {
    }

    public string? MessageId { get; private set; }

    public string? FromAddress { get; private set; }

    public IReadOnlyList<string> ToRecipients => _toRecipients.AsReadOnly();

    public string? Subject { get; private set; }

    public string? TextBody { get; private set; }

    public string? HtmlBody { get; private set; }

    public string RawMime { get; private set; } = string.Empty;

    public InboundMailMessageStatus Status { get; private set; }

    public DateTimeOffset ReceivedAtUtc { get; private set; }

    public static InboundMailMessage Receive(
        string? messageId,
        string? fromAddress,
        IReadOnlyList<string> toRecipients,
        string? subject,
        string? textBody,
        string? htmlBody,
        string rawMime,
        DateTimeOffset receivedAtUtc) {
        if (toRecipients.Count == 0) {
            throw new ArgumentException("At least one recipient is required.", nameof(toRecipients));
        }

        if (string.IsNullOrWhiteSpace(rawMime)) {
            throw new ArgumentException("Raw MIME content is required.", nameof(rawMime));
        }

        var normalizedReceivedAtUtc = receivedAtUtc.ToUniversalTime();
        var message = new InboundMailMessage(InboundMailMessageId.New()) {
            MessageId = NullIfWhiteSpace(messageId),
            FromAddress = NullIfWhiteSpace(fromAddress),
            Subject = NullIfWhiteSpace(subject),
            TextBody = textBody,
            HtmlBody = htmlBody,
            RawMime = rawMime,
            Status = InboundMailMessageStatus.Received,
            ReceivedAtUtc = normalizedReceivedAtUtc
        };
        message._toRecipients.AddRange(toRecipients.Select(NormalizeRecipient));
        message.SetCreated(normalizedReceivedAtUtc.UtcDateTime);
        message.RaiseDomainEvent(new InboundMailMessageReceivedDomainEvent(message.Id, normalizedReceivedAtUtc.UtcDateTime));
        return message;
    }

    public void Archive(DateTimeOffset archivedAtUtc) {
        if (Status == InboundMailMessageStatus.Archived) {
            return;
        }

        Status = InboundMailMessageStatus.Archived;
        SetModified(archivedAtUtc.UtcDateTime);
    }

    private static string NormalizeRecipient(string recipient) {
        if (string.IsNullOrWhiteSpace(recipient)) {
            throw new ArgumentException("Recipient cannot be empty.", nameof(recipient));
        }

        return recipient.Trim();
    }

    private static string? NullIfWhiteSpace(string? value) {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
