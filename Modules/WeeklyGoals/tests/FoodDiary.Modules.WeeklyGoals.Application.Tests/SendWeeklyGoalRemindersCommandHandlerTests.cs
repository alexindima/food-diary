using FoodDiary.Modules.WeeklyGoals.Application.Commands.SendWeeklyGoalReminders;
using FoodDiary.Modules.WeeklyGoals.Contracts.Commands.SendWeeklyGoalReminders;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Modules.Notifications.Contracts.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;
using FoodDiary.Modules.WeeklyGoals.Domain.Entities;
using FoodDiary.Modules.WeeklyGoals.Domain.Enums;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.WeeklyGoals.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class SendWeeklyGoalRemindersCommandHandlerTests {
    private static readonly DateTime WeekStart = new(2026, 8, 10, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime ReminderUtcTime = new(2026, 8, 10, 17, 5, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ProcessAsync_WhenReminderIsDue_SendsOnlyOnceForLocalDate() {
        var goal = WeeklyGoal.Create(
            UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, targetDays: 5,
            reminderEnabled: true, reminderTimeMinutes: 21 * 60, timeZoneOffsetMinutes: 240);
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        repository.GetReminderCandidatesAsync(
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([goal]);
        INotificationWriter notificationWriter = Substitute.For<INotificationWriter>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        var processor = new SendWeeklyGoalRemindersCommandHandler(
            repository,
            notificationWriter,
            unitOfWork,
            new FixedTimeProvider(ReminderUtcTime));

        int firstResult = await processor.Handle(new SendWeeklyGoalRemindersCommand(), CancellationToken.None);
        int secondResult = await processor.Handle(new SendWeeklyGoalRemindersCommand(), CancellationToken.None);

        Assert.Multiple(
            () => Assert.Equal(1, firstResult),
            () => Assert.Equal(0, secondResult),
            () => Assert.Equal(new DateOnly(2026, 8, 10), goal.LastReminderLocalDate));
        await notificationWriter.Received(1).AddAsync(
            Arg.Is<NotificationRequest>(notification => notification.UserId == goal.UserId),
            sendWebPush: true,
            cancellationToken: Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WhenReminderConfigurationIsMissing_DoesNothing() {
        var goal = WeeklyGoal.Create(
            UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, targetDays: 5,
            reminderEnabled: false, reminderTimeMinutes: null, timeZoneOffsetMinutes: null);
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        repository.GetReminderCandidatesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns([goal]);
        INotificationWriter writer = Substitute.For<INotificationWriter>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        var processor = new SendWeeklyGoalRemindersCommandHandler(
            repository, writer, unitOfWork, new FixedTimeProvider(ReminderUtcTime));

        int sent = await processor.Handle(new SendWeeklyGoalRemindersCommand(), CancellationToken.None);

        Assert.Equal(0, sent);
        await writer.DidNotReceiveWithAnyArgs().AddAsync(default!, default, default);
        await unitOfWork.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task ProcessAsync_WhenExecutionIsDelayed_SendsCatchUpReminderForSameLocalDay() {
        var goal = WeeklyGoal.Create(
            UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, targetDays: 5,
            reminderEnabled: true, reminderTimeMinutes: 9 * 60, timeZoneOffsetMinutes: 0);
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        repository.GetReminderCandidatesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns([goal]);
        INotificationWriter writer = Substitute.For<INotificationWriter>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        var processor = new SendWeeklyGoalRemindersCommandHandler(
            repository, writer, unitOfWork, new FixedTimeProvider(WeekStart.AddHours(18)));

        int sent = await processor.Handle(new SendWeeklyGoalRemindersCommand(), CancellationToken.None);

        Assert.Equal(1, sent);
        await writer.Received(1).AddAsync(
            Arg.Any<NotificationRequest>(),
            sendWebPush: true,
            cancellationToken: Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessAsync_WithMoreThanOneBatch_ProcessesEveryCandidate() {
        WeeklyGoal[] goals = [.. Enumerable.Range(0, 501).Select(_ => WeeklyGoal.Create(
            UserId.New(), WeekStart, WeeklyGoalType.DiaryLogging, targetDays: 5,
            reminderEnabled: true, reminderTimeMinutes: 9 * 60, timeZoneOffsetMinutes: 0))];
        IWeeklyGoalRepository repository = Substitute.For<IWeeklyGoalRepository>();
        repository.GetReminderCandidatesAsync(
                Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                int offset = call.ArgAt<int>(2);
                int limit = call.ArgAt<int>(3);
                return Task.FromResult<IReadOnlyList<WeeklyGoal>>([.. goals.Skip(offset).Take(limit)]);
            });
        INotificationWriter writer = Substitute.For<INotificationWriter>();
        IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
        var processor = new SendWeeklyGoalRemindersCommandHandler(
            repository, writer, unitOfWork, new FixedTimeProvider(WeekStart.AddHours(18)));

        int sent = await processor.Handle(new SendWeeklyGoalRemindersCommand(), CancellationToken.None);

        Assert.Equal(501, sent);
        await repository.Received(1).GetReminderCandidatesAsync(
            Arg.Any<DateTime>(), Arg.Any<DateTime>(), 500, 500, Arg.Any<CancellationToken>());
        await unitOfWork.Received(2).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class FixedTimeProvider(DateTime utcNow) : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(utcNow);
    }
}
