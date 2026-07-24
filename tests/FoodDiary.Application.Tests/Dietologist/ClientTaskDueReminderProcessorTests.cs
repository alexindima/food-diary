using FoodDiary.Application.Abstractions.Dietologist.Common;
using FoodDiary.Application.Abstractions.Notifications.Common;
using FoodDiary.Application.Dietologist.Services;
using FoodDiary.Domain.Entities.Dietologist;
using FoodDiary.Domain.Entities.Notifications;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Tests.Dietologist;

[ExcludeFromCodeCoverage]
public sealed class ClientTaskDueReminderProcessorTests {
    [Fact]
    public async Task ProcessAsync_MarksTaskAndCreatesOneNotification() {
        DateTime utcNow = DateTime.UtcNow;
        var task = ClientTask.Create(
            UserId.New(),
            UserId.New(),
            "Due soon",
            details: null,
            dueAtUtc: utcNow.AddHours(12));
        IClientTaskRepository repository = Substitute.For<IClientTaskRepository>();
        repository.GetDueForReminderAsync(
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns(
                new[] { task },
                Array.Empty<ClientTask>());
        INotificationWriter notifications = Substitute.For<INotificationWriter>();
        var processor = new ClientTaskDueReminderProcessor(
            repository,
            notifications,
            new FixedTimeProvider(utcNow));

        int firstCount = await processor.ProcessAsync();
        int secondCount = await processor.ProcessAsync();

        Assert.Multiple(
            () => Assert.Equal(1, firstCount),
            () => Assert.Equal(0, secondCount),
            () => Assert.Equal(utcNow, task.DueReminderSentAtUtc));
        await notifications.Received(1).AddAsync(
            Arg.Is<Notification>(notification => notification != null && notification.UserId == task.ClientUserId),
            sendWebPush: false,
            Arg.Any<CancellationToken>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow, TimeSpan.Zero);
    }
}
