using FoodDiary.Application.Abstractions.Admin.Common;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Admin.Common;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminDashboardOverview;
using FoodDiary.Results;

namespace FoodDiary.Application.Tests.Admin;

[ExcludeFromCodeCoverage]
public sealed class AdminDashboardOverviewTests {
    [Fact]
    public async Task Overview_PropagatesCurrentSummaryFailure() {
        IAdminDashboardMetricsReader reader = Substitute.For<IAdminDashboardMetricsReader>();
        IAdminBillingReadRepository billing = Substitute.For<IAdminBillingReadRepository>();
        IAdminDashboardReadService dashboard = Substitute.For<IAdminDashboardReadService>();
        reader.GetAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(new AdminDashboardMetrics(0, 0, 0, []));
        billing.GetRevenueSummaryAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>()).Returns(new AdminBillingRevenueSummaryReadModel(DateTime.UnixEpoch, DateTime.UnixEpoch, []));
        var error = new Error("dashboard.unavailable", "Unavailable", ErrorKind.Internal);
        dashboard.GetSummaryAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Failure<AdminDashboardSummaryModel>(error));
        var service = new FoodDiary.Application.Admin.Services.AdminDashboardOverviewReadService(reader, billing, dashboard, new Clock());

        Result<AdminDashboardOverviewModel> result = await service.GetAsync(fromDate: null, toDate: null, allTime: true, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task Overview_UsesInclusiveUiDatesAndEqualPreviousRange() {
        IAdminDashboardMetricsReader reader = Substitute.For<IAdminDashboardMetricsReader>();
        IAdminBillingReadRepository billing = Substitute.For<IAdminBillingReadRepository>();
        IAdminDashboardReadService dashboard = Substitute.For<IAdminDashboardReadService>();
        reader.GetAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(new AdminDashboardMetrics(3, 2, 900, []));
        billing.GetRevenueSummaryAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new AdminBillingRevenueSummaryReadModel(DateTime.UnixEpoch, DateTime.UnixEpoch, []));
        dashboard.GetSummaryAsync(1, Arg.Any<CancellationToken>()).Returns(Result.Success(new AdminDashboardSummaryModel(40, 40, 10, 0, 2, [])));
        var handler = new GetAdminDashboardOverviewQueryHandler(new FoodDiary.Application.Admin.Services.AdminDashboardOverviewReadService(reader, billing, dashboard, new Clock()));
        Result<AdminDashboardOverviewModel> result = await handler.Handle(new GetAdminDashboardOverviewQuery(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 7)), CancellationToken.None);
        Assert.True(result.IsSuccess);
        Assert.Equal(new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc), result.Value.ToUtc);
        Assert.NotNull(result.Value.Previous);
        Assert.Equal(new DateTime(2026, 8, 25, 0, 0, 0, DateTimeKind.Utc), result.Value.Previous.FromUtc);
        Assert.Equal(result.Value.FromUtc, result.Value.Previous.ToUtc);
        Assert.Equal(40, result.Value.TotalUsersNow);
        Assert.Equal(3, result.Value.Period.Metrics.Registrations);
        Result<AdminDashboardOverviewModel> all = await handler.Handle(new GetAdminDashboardOverviewQuery(AllTime: true), CancellationToken.None);
        Assert.Null(all.Value.Previous);
        Assert.Equal(DateTime.UnixEpoch, all.Value.FromUtc);
        Assert.Equal("month", all.Value.Interval);
    }

    [Fact]
    public async Task Overview_RejectsReversedOrFutureDatesBeforeReadingData() {
        IAdminDashboardMetricsReader reader = Substitute.For<IAdminDashboardMetricsReader>();
        var handler = new GetAdminDashboardOverviewQueryHandler(new FoodDiary.Application.Admin.Services.AdminDashboardOverviewReadService(reader, Substitute.For<IAdminBillingReadRepository>(), Substitute.For<IAdminDashboardReadService>(), new Clock()));
        Assert.True((await handler.Handle(new GetAdminDashboardOverviewQuery(new DateOnly(2026, 9, 7), new DateOnly(2026, 9, 1)), CancellationToken.None)).IsFailure);
        Assert.True((await handler.Handle(new GetAdminDashboardOverviewQuery(To: new DateOnly(2026, 9, 9)), CancellationToken.None)).IsFailure);
        Assert.True((await handler.Handle(new GetAdminDashboardOverviewQuery(From: new DateOnly(2026, 9, 1), AllTime: true), CancellationToken.None)).IsFailure);
        await reader.DidNotReceive().GetAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<bool>(), Arg.Any<CancellationToken>());
    }

    [ExcludeFromCodeCoverage]
    private sealed class Clock : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 8, 12, 0, 0, TimeSpan.Zero);
    }
}
