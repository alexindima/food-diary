namespace FoodDiary.Modules.Fasting.Contracts.Jobs;

public interface IFastingNotificationScheduler {
    Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default);
}
