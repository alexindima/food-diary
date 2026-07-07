namespace FoodDiary.Infrastructure.Persistence.Outbox;

internal static class OutboxProcessingPolicy {
    public const int MaxAttemptCount = 10;

    private const int MaxErrorLength = 2048;

    public static bool ShouldDeadLetter(int attemptCount) =>
        attemptCount >= MaxAttemptCount;

    public static TimeSpan CalculateRetryDelay(int attemptCount) {
        int minutes = Math.Min(60, (int)Math.Pow(2, Math.Min(attemptCount, 6)));
        return TimeSpan.FromMinutes(minutes);
    }

    public static string TruncateError(string error) =>
        error.Length <= MaxErrorLength ? error : error[..MaxErrorLength];
}