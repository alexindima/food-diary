using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Outbox;

namespace FoodDiary.Infrastructure.Persistence.Achievements;

public sealed class AchievementEvaluationOutboxMessage : IOutboxMessage {
    private const int ErrorMaxLength = 2048;

    public Guid Id { get; private set; }
    public UserId UserId { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime NextAttemptOnUtc { get; private set; }
    public int AttemptCount { get; private set; }
    public DateTime? ProcessedOnUtc { get; private set; }
    public DateTime? DeadLetteredOnUtc { get; private set; }
    public DateTime? LockedUntilUtc { get; private set; }
    public string? LockedBy { get; private set; }
    public string? LastError { get; private set; }
    public long Revision { get; private set; }

    private AchievementEvaluationOutboxMessage() {
    }

    public static AchievementEvaluationOutboxMessage Create(UserId userId, DateTime createdOnUtc) {
        if (userId == UserId.Empty) {
            throw new ArgumentException("UserId is required.", nameof(userId));
        }

        DateTime normalizedCreatedOnUtc = NormalizeUtc(createdOnUtc);
        return new AchievementEvaluationOutboxMessage {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedOnUtc = normalizedCreatedOnUtc,
            NextAttemptOnUtc = normalizedCreatedOnUtc,
            Revision = 1,
        };
    }

    public void RequestEvaluation(DateTime requestedOnUtc) {
        CreatedOnUtc = NormalizeUtc(requestedOnUtc);
        Revision = checked(Revision + 1);
        NextAttemptOnUtc = CreatedOnUtc;
        AttemptCount = 0;
        ProcessedOnUtc = null;
        DeadLetteredOnUtc = null;
        LastError = null;
    }

    public void ReleaseForUpdatedRevision() {
        LockedUntilUtc = null;
        LockedBy = null;
    }

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

    public void MarkDeadLettered(string error, DateTime deadLetteredOnUtc) {
        AttemptCount++;
        DeadLetteredOnUtc = NormalizeUtc(deadLetteredOnUtc);
        LockedUntilUtc = null;
        LockedBy = null;
        LastError = TruncateOptional(error, ErrorMaxLength);
    }

    public void MarkFailed(string error, DateTime nextAttemptOnUtc) {
        AttemptCount++;
        NextAttemptOnUtc = NormalizeUtc(nextAttemptOnUtc);
        LockedUntilUtc = null;
        LockedBy = null;
        LastError = TruncateOptional(error, ErrorMaxLength);
    }

    public void MarkReplayed(DateTime nextAttemptOnUtc) {
        NextAttemptOnUtc = NormalizeUtc(nextAttemptOnUtc);
        DeadLetteredOnUtc = null;
        LockedUntilUtc = null;
        LockedBy = null;
        LastError = null;
    }

    private static string? TruncateOptional(string value, int maxLength) {
        if (string.IsNullOrWhiteSpace(value)) {
            return null;
        }

        string trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static DateTime NormalizeUtc(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : value.ToUniversalTime();
}
