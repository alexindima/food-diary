namespace FoodDiary.JobManager.Services;

public static class RecurringJobExecutionPolicy {
    public const int CleanupRetryAttempts = 3;
    public const int CleanupConcurrencyTimeoutSeconds = 30 * 60;
}
