namespace FoodDiary.Modules.Notifications.Application.Abstractions.Models;

public sealed record WebPushDeliveryOutcome(IReadOnlyCollection<Guid> CompletedSubscriptionIds, int RetryableFailureCount) {
    public bool RequiresRetry => RetryableFailureCount > 0;
    public static WebPushDeliveryOutcome Skipped { get; } = new([], 0);
}
