namespace FoodDiary.MailRelay.Presentation.Features.Email.Responses;

public sealed record MailRelayQueueStatsHttpResponse(
    long PendingCount,
    long RetryCount,
    long ProcessingCount,
    long SentCount,
    long FailedCount,
    long SuppressedCount);
