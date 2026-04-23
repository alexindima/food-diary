namespace FoodDiary.MailRelay.Services;

public sealed record MailRelayQueueStats(
    long PendingCount,
    long RetryCount,
    long ProcessingCount,
    long SentCount,
    long FailedCount,
    long SuppressedCount);
