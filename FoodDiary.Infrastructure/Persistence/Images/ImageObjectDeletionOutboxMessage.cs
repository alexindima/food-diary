namespace FoodDiary.Infrastructure.Persistence.Images;

public sealed class ImageObjectDeletionOutboxMessage {
    private const int ObjectKeyMaxLength = 1024;
    private const int ErrorMaxLength = 2048;

    public Guid Id { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime NextAttemptOnUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public string? LastError { get; private set; }

    private ImageObjectDeletionOutboxMessage() {
    }

    public static ImageObjectDeletionOutboxMessage Create(string objectKey, DateTime createdOnUtc) {
        if (string.IsNullOrWhiteSpace(objectKey)) {
            throw new ArgumentException("Object key is required.", nameof(objectKey));
        }

        string normalizedObjectKey = objectKey.Trim();
        if (normalizedObjectKey.Length > ObjectKeyMaxLength) {
            throw new ArgumentOutOfRangeException(nameof(objectKey), $"Object key must be at most {ObjectKeyMaxLength} characters.");
        }

        DateTime normalizedCreatedOnUtc = NormalizeUtc(createdOnUtc);
        return new ImageObjectDeletionOutboxMessage {
            Id = Guid.NewGuid(),
            ObjectKey = normalizedObjectKey,
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
