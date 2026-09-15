using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Dietologist.Contracts.Commands.SendClientTaskReminders;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Modules.Dietologist.Application.Common;
using FoodDiary.Modules.Dietologist.Domain.Entities;

namespace FoodDiary.Modules.Dietologist.Application.Commands.SendClientTaskReminders;

public sealed class SendClientTaskRemindersCommandHandler(
    IClientTaskWriteRepository taskRepository,
    INotificationWriter notificationWriter,
    TimeProvider timeProvider) : ICommandHandler<SendClientTaskRemindersCommand, int> {
    private const int BatchSize = 100;
    private static readonly TimeSpan ReminderWindow = TimeSpan.FromHours(24);

    public async Task<int> Handle(SendClientTaskRemindersCommand command, CancellationToken cancellationToken) {
        DateTime utcNow = timeProvider.GetUtcNow().UtcDateTime;
        IReadOnlyList<ClientTask> tasks = await taskRepository.GetDueForReminderAsync(
            utcNow,
            utcNow.Add(ReminderWindow),
            BatchSize,
            cancellationToken).ConfigureAwait(false);

        foreach (ClientTask task in tasks) {
            await notificationWriter.AddAsync(
                DietologistNotificationFactory.CreateClientTaskDueSoon(task.ClientUserId),
                cancellationToken: cancellationToken).ConfigureAwait(false);
            task.MarkDueReminderSent(utcNow);
        }

        return tasks.Count;
    }
}
