using FoodDiary.Mediator;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.WeeklyGoals.Application.Abstractions.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Commands.UpsertWeeklyGoal;
using FoodDiary.Modules.WeeklyGoals.Application.Common;
using FoodDiary.Modules.WeeklyGoals.Application.Queries.GetWeeklyGoal;
using FoodDiary.Results;

namespace FoodDiary.Modules.WeeklyGoals.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class InvalidCalendarZoneTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnknownZone_IsRejectedBeforeGoalReadsTransactionsProgressOrClockAccess(bool write) {
        var user = UserId.New();
        using var cancellation = new CancellationTokenSource();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        access.EnsureCanAccessAsync(user, cancellation.Token).Returns((Error?)null);
        IWeeklyGoalRepository goals = Substitute.For<IWeeklyGoalRepository>();
        IWeeklyGoalTransactionRunner transactions = Substitute.For<IWeeklyGoalTransactionRunner>();
        ISender activity = Substitute.For<ISender>();
        var progress = new WeeklyGoalProgressReader(activity);
        TimeProvider clock = Substitute.For<TimeProvider>();
        var week = new DateOnly(2026, 10, 19);
        const string zone = "FoodDiary/Unknown";

        Error error = write
            ? ResultAssert.Failure(await new UpsertWeeklyGoalCommandHandler(goals, transactions, progress, access, clock).Handle(
                new UpsertWeeklyGoalCommand(user.Value, week, 3, ReminderEnabled: false, ReminderTime: null, TimeZoneOffsetMinutes: null, zone), cancellation.Token))
            : ResultAssert.Failure(await new GetWeeklyGoalQueryHandler(goals, progress, access).Handle(
                new GetWeeklyGoalQuery(user.Value, week, zone), cancellation.Token));

        Assert.Multiple(
            () => Assert.Equal("Validation.Invalid", error.Code),
            () => Assert.Contains("calendar time zone", error.Message, StringComparison.Ordinal),
            () => Assert.Empty(goals.ReceivedCalls()),
            () => Assert.Empty(transactions.ReceivedCalls()),
            () => Assert.Empty(activity.ReceivedCalls()),
            () => Assert.Empty(clock.ReceivedCalls()));
        await access.Received(1).EnsureCanAccessAsync(user, cancellation.Token);
    }
}
