using System.Globalization;
using System.Text;
using FoodDiary.Modules.Users.Application.Queries.GetWaistGoalHistoryPage;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Users.Application.Tests;

[ExcludeFromCodeCoverage]
public sealed class WaistGoalHistoryPageTests {
    private static readonly DateTime Now = new(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(10, false)]
    [InlineData(11, true)]
    public async Task FirstPage_IsBoundedAndOnlyOffersCursorWhenAnotherRowExists(int count, bool hasMore) {
        IUserBodyMetricHistoryReadService reader = Substitute.For<IUserBodyMetricHistoryReadService>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        var id = UserId.New();
        using var cancellation = new CancellationTokenSource();
        WaistGoalHistoryModel[] rows = [.. Enumerable.Range(0, count).Select(index => new WaistGoalHistoryModel(
            Guid.NewGuid(), 70, 80, 75, Now.AddDays(-index - 1), Now, "Cancelled"))];
        reader.ReadWaistGoalsAsync(id, Now, 0, 11, cancellation.Token)
            .Returns(Result.Success<IReadOnlyList<WaistGoalHistoryModel>>(rows));
        var handler = new GetWaistGoalHistoryPageQueryHandler(reader, access, new FixedClock());
        GoalHistoryPageModel<WaistGoalHistoryModel> page = ResultAssert.Success(await handler.Handle(
            new GetWaistGoalHistoryPageQuery(id.Value), cancellation.Token));
        Assert.Equal(rows.Take(10), page.Items);
        Assert.Equal(hasMore, page.NextCursor is not null);
        await access.Received(1).EnsureCanAccessAsync(id, cancellation.Token);
        if (hasMore) {
            reader.ReadWaistGoalsAsync(id, Now, 10, 11, cancellation.Token)
                .Returns(Result.Success<IReadOnlyList<WaistGoalHistoryModel>>([]));
            GoalHistoryPageModel<WaistGoalHistoryModel> next = ResultAssert.Success(await handler.Handle(
                new GetWaistGoalHistoryPageQuery(id.Value, page.NextCursor), cancellation.Token));
            Assert.Empty(next.Items);
            Assert.Null(next.NextCursor);
            await reader.Received(1).ReadWaistGoalsAsync(id, Now, 10, 11, cancellation.Token);
        }
    }

    [Theory]
    [InlineData("not-base64")]
    [InlineData("")]
    [InlineData("future")]
    [InlineData("negative")]
    [InlineData("overflow")]
    [InlineData("malformed")]
    [InlineData("long")]
    [InlineData("negative-time")]
    public async Task InvalidCursor_IsRejectedBeforeReadingGoals(string kind) {
        string cursor = kind switch {
            "future" => Encode(Now.AddDays(1).Ticks.ToString(CultureInfo.InvariantCulture) + ":0"),
            "negative" => Encode(Now.Ticks.ToString(CultureInfo.InvariantCulture) + ":-1"),
            "overflow" => Encode(Now.Ticks.ToString(CultureInfo.InvariantCulture) + ":2147483647"),
            "malformed" => Encode("1:2:3"),
            "long" => new string('a', 129),
            "negative-time" => Encode("-1:0"),
            _ => kind,
        };
        IUserBodyMetricHistoryReadService reader = Substitute.For<IUserBodyMetricHistoryReadService>();
        var handler = new GetWaistGoalHistoryPageQueryHandler(reader, Substitute.For<ICurrentUserAccessService>(), new FixedClock());
        ResultAssert.Failure(await handler.Handle(new GetWaistGoalHistoryPageQuery(Guid.NewGuid(), cursor), CancellationToken.None));
        Assert.Empty(reader.ReceivedCalls());
    }

    [Fact]
    public async Task AccessAndReadFailures_ArePropagated() {
        IUserBodyMetricHistoryReadService reader = Substitute.For<IUserBodyMetricHistoryReadService>();
        ICurrentUserAccessService access = Substitute.For<ICurrentUserAccessService>();
        var handler = new GetWaistGoalHistoryPageQueryHandler(reader, access, new FixedClock());
        ResultAssert.Failure(await handler.Handle(new GetWaistGoalHistoryPageQuery(UserId: null), CancellationToken.None));
        Assert.Empty(reader.ReceivedCalls());
        var id = UserId.New();
        var error = new Error("Denied", "Denied");
        access.EnsureCanAccessAsync(id, Arg.Any<CancellationToken>()).Returns(error);
        ResultAssert.Failure(await handler.Handle(new GetWaistGoalHistoryPageQuery(id.Value), CancellationToken.None), error.Code);
        Assert.Empty(reader.ReceivedCalls());
        access.EnsureCanAccessAsync(id, Arg.Any<CancellationToken>()).Returns((Error?)null);
        reader.ReadWaistGoalsAsync(id, Now, 0, 11, Arg.Any<CancellationToken>())
            .Returns(Result.Failure<IReadOnlyList<WaistGoalHistoryModel>>(error));
        ResultAssert.Failure(await handler.Handle(new GetWaistGoalHistoryPageQuery(id.Value), CancellationToken.None), error.Code);
    }

    private static string Encode(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value));

    [ExcludeFromCodeCoverage]
    private sealed class FixedClock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Now);
    }
}
