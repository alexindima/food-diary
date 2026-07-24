using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Notifications.Common;
using FoodDiary.Domain.Entities.Dietologist;

namespace FoodDiary.Application.Dietologist.Services;

public sealed class ClientTaskDueReminderProcessor(
    IClientTaskRepository taskRepository,
    INotificationWriter notificationWriter,
    TimeProvider timeProvider) {
    private const int BatchSize = 100;
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

    public async Task<int> ProcessAsync(CancellationToken cancellationToken = default) {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<ClientTask> tasks = await taskRepository.GetDueForReminderAsync(
            utcNow,
            utcNow.Add(ReminderWindow),
            BatchSize,
            cancellationToken).ConfigureAwait(false);

        foreach (ClientTask task in tasks) {
            await notificationWriter.AddAsync(
                NotificationFactory.CreateClientTaskDueSoon(task.ClientUserId),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            task.MarkDueReminderSent(utcNow);
        }

        return tasks.Count;
    }
}
