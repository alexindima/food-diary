using System.Text;

namespace FoodDiary.MailRelay.Domain.Emails;

public static class RelayEmailMessageLimits {
    public const int MaximumRecipients = 100;
    public const int MaximumAddressLength = 320;
    public const int MaximumNameLength = 256;
    public const int MaximumSubjectLength = 998;
    public const int MaximumIdentifierLength = 256;
    public const int MaximumHeaderLength = 998;
    public const int MaximumBodyBytes = 1024 * 1024;
    public const int MaximumRequestBytes = 8 * 1024 * 1024;

    public static bool IsWithinBounds(RelayEmailMessageRequest request) {
        if (request.To is null || request.To.Count is < 1 or > MaximumRecipients ||
            !IsBounded(request.FromAddress, MaximumAddressLength) ||
            !IsBounded(request.FromName, MaximumNameLength) ||
            !IsBounded(request.Subject, MaximumSubjectLength) ||
            !IsBounded(request.HtmlBody, MaximumBodyBytes) ||
            !IsBounded(request.TextBody, MaximumBodyBytes) ||
            !IsBounded(request.CorrelationId, MaximumIdentifierLength) ||
            !IsBounded(request.IdempotencyKey, MaximumIdentifierLength) ||
            !IsBounded(request.MessageId, MaximumHeaderLength) ||
            !IsBounded(request.Purpose, 64) ||
            !IsBounded(request.ReplyTo, MaximumAddressLength) ||
            !IsBounded(request.InReplyTo, MaximumHeaderLength)) {
            return false;
        }

        return request.To.All(address => IsBounded(address, MaximumAddressLength)) &&
               Encoding.UTF8.GetByteCount(request.HtmlBody ?? string.Empty) +
               Encoding.UTF8.GetByteCount(request.TextBody ?? string.Empty) <= MaximumBodyBytes;
    }

    private static bool IsBounded(string? value, int maximumLength) => value is null || value.Length <= maximumLength;
}
