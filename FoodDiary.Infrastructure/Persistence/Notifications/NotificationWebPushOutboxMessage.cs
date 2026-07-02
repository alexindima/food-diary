using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Infrastructure.Persistence.Notifications;

public sealed class NotificationWebPushOutboxMessage {
    private const int ErrorMaxLength = 2048;

    public Guid Id { get; private set; }
    public NotificationId NotificationId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime NextAttemptOnUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? LastError { get; private set; }

    public Notification Notification { get; } = null!;

    private NotificationWebPushOutboxMessage() {
    }

    public static NotificationWebPushOutboxMessage Create(NotificationId notificationId, DateTime createdOnUtc) {
        if (notificationId == NotificationId.Empty) {
            throw new ArgumentException("NotificationId is required.", nameof(notificationId));
        }

        DateTime normalizedCreatedOnUtc = NormalizeUtc(createdOnUtc);
        return new NotificationWebPushOutboxMessage {
            Id = Guid.NewGuid(),
            NotificationId = notificationId,
            CreatedOnUtc = normalizedCreatedOnUtc,
            NextAttemptOnUtc = normalizedCreatedOnUtc,
        };
    }

    public void MarkProcessed(DateTime processedOnUtc) {
        ProcessedOnUtc = NormalizeUtc(processedOnUtc);
        LastError = null;
    }

    public void MarkFailed(string error, DateTime nextAttemptOnUtc) {
        AttemptCount++;
        NextAttemptOnUtc = NormalizeUtc(nextAttemptOnUtc);
        LastError = string.IsNullOrWhiteSpace(error)
            ? null
            : error.Trim()[..Math.Min(error.Trim().Length, ErrorMaxLength)];
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc
            ? value
            : value.ToUniversalTime();
}
