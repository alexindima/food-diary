using FoodDiary.Modules.Users.Application.Abstractions.Common;
using FoodDiary.Modules.Users.Application.Commands.InitializePendingBodyGoals;
using FoodDiary.Modules.Users.Application.Commands.PurgeOwnedFixtureUsers;
using FoodDiary.Modules.Users.Application.Common;
using FoodDiary.Modules.Users.Application.Queries.GetWaistGoalHistory;
using FoodDiary.Modules.Users.Application.Queries.GetWeightGoalHistory;
using FoodDiary.Modules.Users.Contracts.Commands.InitializePendingBodyGoals;
using FoodDiary.Modules.Users.Contracts.Commands.PurgeOwnedFixtureUsers;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Entities.Tracking;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class UserHistoryAndInitializationBoundaryTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WeightHistoryHandler_ReturnsTheAccessibleUsersOrderedHistoryWithoutWriting(bool withGoals) {
        var user = User.Create("weight-history@example.test", "hash");
        var first = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime second = first.AddDays(2);
        WeightGoalHistoryModel[] expected = [];
        if (withGoals) {
            WeightGoal oldGoal = user.StartWeightGoal(75, 82, first);
            WeightGoal activeGoal = user.StartWeightGoal(72, 80, second);
            expected = [
                new(activeGoal.Id.Value, 72, 80, EndWeightKg: null, second, EndedAtUtc: null, "Active"),
                new(oldGoal.Id.Value, 75, 82, 80, first, second, "Replaced"),
            ];
        }
        using var cancellation = new CancellationTokenSource();
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        lookup.GetByIdAsync(user.Id, cancellation.Token).Returns(user);
        var service = new UserContextService(lookup, writer);

        IReadOnlyList<WeightGoalHistoryModel> rows = ResultAssert.Success(await new GetWeightGoalHistoryQueryHandler(service, service)
            .Handle(new GetWeightGoalHistoryQuery(user.Id.Value), cancellation.Token));

        Assert.Equal(expected, rows);
        await lookup.Received().GetByIdAsync(user.Id, cancellation.Token);
        Assert.Empty(writer.ReceivedCalls());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task WaistHistoryHandler_ReturnsTheAccessibleUsersOrderedHistoryWithoutWriting(bool withGoals) {
        var user = User.Create("waist-history@example.test", "hash");
        var first = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        DateTime second = first.AddDays(2);
        WaistGoalHistoryModel[] expected = [];
        if (withGoals) {
            WaistGoal oldGoal = user.StartWaistGoal(80, 90, first);
            WaistGoal activeGoal = user.StartWaistGoal(76, 86, second);
            expected = [
                new(activeGoal.Id.Value, 76, 86, EndWaistCm: null, second, EndedAtUtc: null, "Active"),
                new(oldGoal.Id.Value, 80, 90, 86, first, second, "Replaced"),
            ];
        }
        using var cancellation = new CancellationTokenSource();
        IUserLookupRepository lookup = Substitute.For<IUserLookupRepository>();
        IUserWriteRepository writer = Substitute.For<IUserWriteRepository>();
        lookup.GetByIdAsync(user.Id, cancellation.Token).Returns(user);
        var service = new UserContextService(lookup, writer);

        IReadOnlyList<WaistGoalHistoryModel> rows = ResultAssert.Success(await new GetWaistGoalHistoryQueryHandler(service, service)
            .Handle(new GetWaistGoalHistoryQuery(user.Id.Value), cancellation.Token));

        Assert.Equal(expected, rows);
        await lookup.Received().GetByIdAsync(user.Id, cancellation.Token);
        Assert.Empty(writer.ReceivedCalls());
    }

    [Fact]
    public async Task PendingGoals_WhenAccessIsDenied_ReturnFalseBeforeReadingTheClockOrWriting() {
        var userId = UserId.New();
        using var cancellation = new CancellationTokenSource();
        IUserContextService users = Substitute.For<IUserContextService>();
        users.GetAccessibleUserAsync(userId, cancellation.Token).Returns(Result.Failure<User>(new Error("Users.AccessDenied", "Denied")));
        TimeProvider clock = Substitute.For<TimeProvider>();
        var command = new InitializePendingBodyGoalsCommand(userId, MeasuredWeightKg.Create(80), MeasuredWaistCm.Create(90));

        bool changed = await new InitializePendingBodyGoalsCommandHandler(users, clock).Handle(command, cancellation.Token);

        Assert.False(changed);
        Assert.Empty(clock.ReceivedCalls());
        await users.Received(1).GetAccessibleUserAsync(userId, cancellation.Token);
        Assert.Single(users.ReceivedCalls());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(2)]
    public async Task FixturePurge_ForwardsOnlyTheExplicitRunScopeAndReturnsTheCleanupCount(int removedCount) {
        var runId = Guid.NewGuid();
        Guid[] ownedUsers = [Guid.NewGuid(), Guid.NewGuid()];
        var cutoff = new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc);
        using var cancellation = new CancellationTokenSource();
        IUserCleanupService cleanup = Substitute.For<IUserCleanupService>();
        cleanup.CleanupOwnedFixtureUsersAsync(runId, ownedUsers, cutoff, cancellation.Token).Returns(removedCount);
        var command = new PurgeOwnedFixtureUsersCommand(runId, ownedUsers, cutoff);

        int result = await new PurgeOwnedFixtureUsersCommandHandler(cleanup).Handle(command, cancellation.Token);

        Assert.Equal(removedCount, result);
        await cleanup.Received(1).CleanupOwnedFixtureUsersAsync(runId, ownedUsers, cutoff, cancellation.Token);
        Assert.Single(cleanup.ReceivedCalls());
    }
}
