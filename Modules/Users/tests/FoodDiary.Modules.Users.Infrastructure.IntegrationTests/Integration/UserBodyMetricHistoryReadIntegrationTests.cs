using FoodDiary.Testing.Assertions;
using System.Data.Common;
using FoodDiary.Infrastructure.IntegrationTests.Integration;
using FoodDiary.Infrastructure.Persistence;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Infrastructure.Persistence.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FoodDiary.Modules.Users.Infrastructure.IntegrationTests.Integration;

[Collection(PostgresDatabaseCollection.Name)]
[ExcludeFromCodeCoverage]
public sealed class UserBodyMetricHistoryReadIntegrationTests(PostgresDatabaseFixture databaseFixture) {
    private static readonly DateTime Start = new(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);

    [RequiresDockerFact]
    public async Task PageProfiles_ReadOnlyActiveAndLatestClosedGoalsUsingBoundedSql() {
        await using FoodDiaryDbContext seed = await databaseFixture.CreateDbContextAsync();
        User user = CreateHistory("bounded-goals@example.com", 25);
        user.UpdatePersonalInfo(height: 180);
        seed.Users.Add(user);
        await seed.SaveChangesAsync();
        var capture = new QueryCapture();
        DbContextOptions<FoodDiaryDbContext> options = new DbContextOptionsBuilder<FoodDiaryDbContext>()
            .UseNpgsql(seed.Database.GetConnectionString()).AddInterceptors(capture).Options;
        await using var context = new FoodDiaryDbContext(options);
        UserBodyMetricHistoryReadService reader = CreateReader(context);

        WeightHistoryProfileModel weight = ResultAssert.Success(await reader.GetWeightHistoryProfileAsync(user.Id, CancellationToken.None));
        WaistHistoryProfileModel waist = ResultAssert.Success(await reader.GetWaistHistoryProfileAsync(user.Id, CancellationToken.None));

        Assert.Multiple(
            () => Assert.Equal(2, weight.GoalHistory.Count),
            () => Assert.Equal(2, waist.GoalHistory.Count),
            () => Assert.Equal("Active", weight.GoalHistory[0].Status),
            () => Assert.Equal("Active", waist.GoalHistory[0].Status),
            () => Assert.Equal(Start.AddDays(24), weight.GoalHistory[1].StartedAtUtc),
            () => Assert.Equal(Start.AddDays(24), waist.GoalHistory[1].StartedAtUtc),
            () => Assert.Equal(180, weight.HeightCm),
            () => Assert.Equal(180, waist.HeightCm),
            () => Assert.Equal(70, weight.Goal.DesiredWeightKg),
            () => Assert.Equal(75, waist.Goal.DesiredWaistCm),
            () => Assert.Empty(context.ChangeTracker.Entries()));
        string[] goalQueries = [.. capture.Commands.Where(sql => sql.Contains("FROM \"WeightGoals\"", StringComparison.Ordinal)
            || sql.Contains("FROM \"WaistGoals\"", StringComparison.Ordinal))];
        Assert.Equal(4, goalQueries.Length);
        Assert.All(goalQueries, sql => Assert.Contains("LIMIT", sql, StringComparison.Ordinal));
        Assert.DoesNotContain(capture.Commands, sql => sql.Contains("Password", StringComparison.Ordinal));
    }

    [RequiresDockerFact]
    public async Task ClosedPages_AreOrderedBoundedIsolatedAndStableWhenActiveGoalCloses() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        User user = CreateHistory("paged-goals@example.com", 23, identicalDates: true);
        User other = CreateHistory("other-paged-goals@example.com", 5);
        context.Users.AddRange(user, other);
        await context.SaveChangesAsync();
        UserBodyMetricHistoryReadService reader = CreateReader(context);
        DateTime snapshot = Start.AddDays(40);
        var weights = new List<WeightGoalHistoryModel>();
        var waists = new List<WaistGoalHistoryModel>();
        weights.AddRange(ResultAssert.Success(await reader.ReadWeightGoalsAsync(user.Id, snapshot, 0, 10, CancellationToken.None)));
        waists.AddRange(ResultAssert.Success(await reader.ReadWaistGoalsAsync(user.Id, snapshot, 0, 10, CancellationToken.None)));
        Assert.Equal(10, weights.Count);
        Assert.Equal(10, waists.Count);
        user.CancelWeightGoal(snapshot.AddSeconds(1), 80);
        user.CancelWaistGoal(snapshot.AddSeconds(1), 85);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        foreach (int offset in new[] { 10, 20 }) {
            weights.AddRange(ResultAssert.Success(await reader.ReadWeightGoalsAsync(user.Id, snapshot, offset, 10, CancellationToken.None)));
            waists.AddRange(ResultAssert.Success(await reader.ReadWaistGoalsAsync(user.Id, snapshot, offset, 10, CancellationToken.None)));
        }
        Assert.Multiple(
            () => Assert.Equal(23, weights.Count),
            () => Assert.Equal(23, weights.Select(goal => goal.Id).Distinct().Count()),
            () => Assert.Equal(23, waists.Count),
            () => Assert.Equal(23, waists.Select(goal => goal.Id).Distinct().Count()),
            () => Assert.All(weights, goal => Assert.Equal(Start, goal.StartedAtUtc)),
            () => Assert.All(waists, goal => Assert.Equal(Start, goal.StartedAtUtc)),
            () => Assert.Empty(context.ChangeTracker.Entries()));
        Assert.Equal(user.WeightGoals.Where(goal => goal.EndedAtUtc <= snapshot).Select(goal => goal.Id.Value).OrderDescending(),
            weights.Select(goal => goal.Id));
        Assert.Equal(user.WaistGoals.Where(goal => goal.EndedAtUtc <= snapshot).Select(goal => goal.Id.Value).OrderDescending(),
            waists.Select(goal => goal.Id));
        Assert.Empty(ResultAssert.Success(await reader.ReadWeightGoalsAsync(user.Id, snapshot, 30, 10, CancellationToken.None)));
        Assert.Empty(ResultAssert.Success(await reader.ReadWaistGoalsAsync(user.Id, snapshot, 30, 10, CancellationToken.None)));
        Assert.Equal(24, ResultAssert.Success(await reader.ReadWeightGoalsAsync(user.Id, snapshot.AddDays(1), 0, 30, CancellationToken.None)).Count);
        Assert.Equal(24, ResultAssert.Success(await reader.ReadWaistGoalsAsync(user.Id, snapshot.AddDays(1), 0, 30, CancellationToken.None)).Count);
    }

    [RequiresDockerFact]
    public async Task ProfilesAndPages_RejectMissingDeletedInactiveUsersAndHonorCancellation() {
        await using FoodDiaryDbContext context = await databaseFixture.CreateDbContextAsync();
        User inactive = CreateHistory("inactive-goals@example.com", 1);
        User deleted = CreateHistory("deleted-goals@example.com", 1);
        var empty = User.Create("empty-goals@example.com", "hash");
        inactive.Deactivate();
        deleted.MarkDeleted(Start.AddDays(40));
        context.Users.AddRange(inactive, deleted, empty);
        await context.SaveChangesAsync();
        UserBodyMetricHistoryReadService reader = CreateReader(context);
        foreach (UserId id in new[] { inactive.Id, deleted.Id, UserId.New() }) {
            ResultAssert.Failure(await reader.GetWeightHistoryProfileAsync(id, CancellationToken.None), "Authentication.InvalidToken");
            ResultAssert.Failure(await reader.GetWaistHistoryProfileAsync(id, CancellationToken.None), "Authentication.InvalidToken");
            ResultAssert.Failure(await reader.ReadWeightGoalsAsync(id, Start, 0, 10, CancellationToken.None), "Authentication.InvalidToken");
            ResultAssert.Failure(await reader.ReadWaistGoalsAsync(id, Start, 0, 10, CancellationToken.None), "Authentication.InvalidToken");
        }
        Assert.Empty(ResultAssert.Success(await reader.GetWeightHistoryProfileAsync(empty.Id, CancellationToken.None)).GoalHistory);
        Assert.Empty(ResultAssert.Success(await reader.GetWaistHistoryProfileAsync(empty.Id, CancellationToken.None)).GoalHistory);
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetWeightHistoryProfileAsync(empty.Id, cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.GetWaistHistoryProfileAsync(empty.Id, cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadWeightGoalsAsync(empty.Id, Start, 0, 10, cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => reader.ReadWaistGoalsAsync(empty.Id, Start, 0, 10, cancelled.Token));
    }

    private static UserBodyMetricHistoryReadService CreateReader(FoodDiaryDbContext context) =>
        new(context.Users, context.WeightGoals, context.WaistGoals);

    private static User CreateHistory(string email, int closedCount, bool identicalDates = false) {
        var user = User.Create(email, "hash");
        for (int index = 0; index < closedCount; index++) {
            DateTime started = identicalDates ? Start : Start.AddDays(index);
            user.StartWeightGoal(70, 90, started);
            user.StartWaistGoal(75, 95, started);
            user.CancelWeightGoal(started, 80);
            user.CancelWaistGoal(started, 85);
        }
        user.StartWeightGoal(70, 80, Start.AddDays(closedCount));
        user.StartWaistGoal(75, 85, Start.AddDays(closedCount));
        return user;
    }

    [ExcludeFromCodeCoverage]
    private sealed class QueryCapture : DbCommandInterceptor {
        public List<string> Commands { get; } = [];
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default) {
            Commands.Add(command.CommandText);
            return ValueTask.FromResult(result);
        }
    }
}
