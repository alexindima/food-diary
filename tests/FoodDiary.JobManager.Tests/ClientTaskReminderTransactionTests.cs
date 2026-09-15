using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Application.Runtime;
using FoodDiary.Mediator;
using FoodDiary.Modules.Dietologist.Application;
using FoodDiary.Modules.Dietologist.Application.Abstractions.Common;
using FoodDiary.Modules.Dietologist.Contracts.Commands.SendClientTaskReminders;
using FoodDiary.Modules.Dietologist.Domain.Entities;
using FoodDiary.Domain.ValueObjects.Ids;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace FoodDiary.JobManager.Tests;

[ExcludeFromCodeCoverage]
public sealed class ClientTaskReminderTransactionTests {
    [Fact]
    public async Task Send_CommitsNotificationAndReminderMarkThroughRuntimePipeline() {
        DateTime now = DateTime.UtcNow;
        var task = ClientTask.Create(UserId.New(), UserId.New(), "Reminder", details: null, dueAtUtc: now.AddHours(1));
        IClientTaskWriteRepository tasks = Substitute.For<IClientTaskWriteRepository>();
        tasks.GetDueForReminderAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), limit: 100, Arg.Any<CancellationToken>())
            .Returns([task]);
        INotificationWriter notifications = Substitute.For<INotificationWriter>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        unitOfWork.HasPendingChanges.Returns(returnThis: true);
        bool notificationStaged = false;
        notifications.AddAsync(Arg.Any<NotificationRequest>(), sendWebPush: false, Arg.Any<CancellationToken>())
            .Returns(_ => { notificationStaged = true; return Task.CompletedTask; });
        unitOfWork.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(_ => {
            Assert.True(notificationStaged);
            Assert.NotNull(task.DueReminderSentAtUtc);
            return Task.CompletedTask;
        });
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplicationRuntime();
        services.AddDietologistApplication();
        services.AddScoped(_ => tasks);
        services.AddScoped(_ => notifications);
        services.AddScoped(_ => unitOfWork);
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        using var cancellation = new CancellationTokenSource();

        int count = await scope.ServiceProvider.GetRequiredService<ISender>()
            .Send(new SendClientTaskRemindersCommand(), cancellation.Token);

        Assert.Equal(1, count);
        await unitOfWork.Received(1).SaveChangesAsync(cancellation.Token);
    }
}
