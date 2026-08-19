using FoodDiary.MailInbox.Domain.Messages;

namespace FoodDiary.MailInbox.Infrastructure.Services;

public static class MailInboxStoredMessageLimits {
    public const int MaxMessageIdCharacters = 998;
    public const int MaxMailboxAddressCharacters = 320;
    public const int MaxSubjectCharacters = 998;
    public const int MaxRecipients = 100;

    public static bool IsWithinLimits(
        string? messageId,
        string? fromAddress,
        IReadOnlyCollection<string> recipients,
        string? subject) =>
        HasMaximumLength(messageId, MaxMessageIdCharacters) &&
        HasMaximumLength(fromAddress, MaxMailboxAddressCharacters) &&
        HasMaximumLength(subject, MaxSubjectCharacters) &&
        recipients.Count is > 0 and <= MaxRecipients &&
        recipients.All(static recipient =>
            !string.IsNullOrWhiteSpace(recipient) &&
            recipient.Length <= MaxMailboxAddressCharacters);

    public static void ThrowIfInvalid(InboundMailMessage message) {
        ArgumentNullException.ThrowIfNull(message);
        if (!IsWithinLimits(message.MessageId, message.FromAddress, message.ToRecipients, message.Subject)) {
            throw new ArgumentException("Inbound mail metadata exceeds the persisted field limits.", nameof(message));
        }
    }

    private static bool HasMaximumLength(string? value, int maximumLength) =>
        value is null || value.Length <= maximumLength;
}
