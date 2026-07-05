using System.Text.Json;
using FoodDiary.Application.Abstractions.Email.Common;

namespace FoodDiary.Infrastructure.Persistence.Email;

public sealed class EmailOutboxMessage {
    private const int ErrorMaxLength = 2048;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Guid Id { get; private set; }
    public string FromAddress { get; private set; } = string.Empty;
    public string FromName { get; private set; } = string.Empty;
    public string ToAddressesJson { get; private set; } = "[]";
    public string Subject { get; private set; } = string.Empty;
    public string HtmlBody { get; private set; } = string.Empty;
    public string? TextBody { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime NextAttemptOnUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public string? LockedBy { get; private set; }
    public string? LastError { get; private set; }

    private EmailOutboxMessage() {
    }

    public static EmailOutboxMessage Create(EmailMessage message, DateTime createdOnUtc) {
        ArgumentNullException.ThrowIfNull(message);

        if (string.IsNullOrWhiteSpace(message.FromAddress)) {
            throw new ArgumentException("From address is required.", nameof(message));
        }

        if (message.ToAddresses.Count == 0 || message.ToAddresses.Any(string.IsNullOrWhiteSpace)) {
            throw new ArgumentException("At least one recipient is required.", nameof(message));
        }

        if (string.IsNullOrWhiteSpace(message.Subject)) {
            throw new ArgumentException("Subject is required.", nameof(message));
        }

        DateTime normalizedCreatedOnUtc = NormalizeUtc(createdOnUtc);
        return new EmailOutboxMessage {
            Id = Guid.NewGuid(),
            FromAddress = message.FromAddress.Trim(),
            FromName = message.FromName.Trim(),
            ToAddressesJson = JsonSerializer.Serialize(message.ToAddresses.Select(static value => value.Trim()).ToArray(), JsonOptions),
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
            CreatedOnUtc = normalizedCreatedOnUtc,
            NextAttemptOnUtc = normalizedCreatedOnUtc,
        };
    }

    public EmailMessage ToEmailMessage() =>
        new(
            FromAddress,
            FromName,
            JsonSerializer.Deserialize<string[]>(ToAddressesJson, JsonOptions) ?? [],
            Subject,
            HtmlBody,
            TextBody);

    public void MarkClaimed(DateTime lockedUntilUtc, string lockedBy) {
        LockedUntilUtc = NormalizeUtc(lockedUntilUtc);
        LockedBy = TruncateOptional(lockedBy, maxLength: 128);
    }

    public void MarkProcessed(DateTime processedOnUtc) {
        ProcessedOnUtc = NormalizeUtc(processedOnUtc);
        LockedUntilUtc = null;
        LockedBy = null;
        LastError = null;
    }

    public void MarkFailed(string error, DateTime nextAttemptOnUtc) {
        AttemptCount++;
        NextAttemptOnUtc = NormalizeUtc(nextAttemptOnUtc);
        LockedUntilUtc = null;
        LockedBy = null;
        LastError = TruncateOptional(error, ErrorMaxLength);
    }

    private static string? TruncateOptional(string value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
}
