namespace FoodDiary.Application.Fasting.Services;

public interface IFastingNotificationScheduler {
    Task<int> ProcessDueNotificationsAsync(CancellationToken cancellationToken = default);
}
