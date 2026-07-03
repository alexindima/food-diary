using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Dashboard;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class DashboardReadServiceTests {
    [Fact]
    public async Task GetSnapshotDataAsync_WhenDailyStatisticsFails_ReturnsFailure() {
        Error error = Errors.Validation.Invalid("statistics", "Statistics failed.");
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        statisticsReadService
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(error)));
        DashboardReadService service = CreateService(statisticsReadService);

        Result<DashboardReadModel> result = await service.GetSnapshotDataAsync(
            UserId.New(),
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            periodDays: 1,
            page: 1,
            pageSize: 10,
            Sections(includeStatistics: true),
            CancellationToken.None);

        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetSnapshotDataAsync_WhenWeeklyStatisticsFails_ReturnsFailure() {
        Error error = Errors.Validation.Invalid("weeklyStatistics", "Weekly statistics failed.");
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        statisticsReadService
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([])),
                Task.FromResult(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(error)));
        DashboardReadService service = CreateService(statisticsReadService);

        Result<DashboardReadModel> result = await service.GetSnapshotDataAsync(
            UserId.New(),
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            periodDays: 1,
            page: 1,
            pageSize: 10,
            Sections(includeStatistics: true),
            CancellationToken.None);

        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetSnapshotDataAsync_WhenMealsFails_ReturnsFailure() {
        Error error = Errors.Validation.Invalid("meals", "Meals failed.");
        IDashboardMealsReadService mealsReadService = Substitute.For<IDashboardMealsReadService>();
        mealsReadService
            .GetMealsAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<DashboardMealsReadModel>(error)));
        DashboardReadService service = CreateService(mealsReadService: mealsReadService);

        Result<DashboardReadModel> result = await service.GetSnapshotDataAsync(
            UserId.New(),
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date,
            periodDays: 1,
            page: 1,
            pageSize: 10,
            Sections(includeMeals: true),
            CancellationToken.None);

        Assert.Equal(error, result.Error);
    }

    private static DashboardReadService CreateService(
        IDashboardStatisticsReadService? statisticsReadService = null,
        IDashboardMealsReadService? mealsReadService = null) {
        IDashboardStatisticsReadService resolvedStatisticsReadService = statisticsReadService ?? CreateSuccessfulStatisticsReadService();
        IDashboardBodyReadService bodyReadService = Substitute.For<IDashboardBodyReadService>();
        bodyReadService
            .GetBodyAsync(
                Arg.Any<UserId>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>(),
                Arg.Any<int>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<bool>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new DashboardBodyReadModel([], [], [], [], HydrationTotalMl: 0)));
        IDashboardMealsReadService resolvedMealsReadService = mealsReadService ?? CreateSuccessfulMealsReadService();

        return new DashboardReadService(
            resolvedStatisticsReadService,
            bodyReadService,
            resolvedMealsReadService);
    }

    private static IDashboardStatisticsReadService CreateSuccessfulStatisticsReadService() {
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        statisticsReadService
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([])));
        return statisticsReadService;
    }

    private static IDashboardMealsReadService CreateSuccessfulMealsReadService() {
        IDashboardMealsReadService mealsReadService = Substitute.For<IDashboardMealsReadService>();
        mealsReadService
            .GetMealsAsync(Arg.Any<UserId>(), Arg.Any<int>(), Arg.Any<int>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success(new DashboardMealsReadModel([], Page: 1, Limit: 10, TotalPages: 0, TotalItems: 0))));
        return mealsReadService;
    }

    private static DashboardReadSections Sections(bool includeStatistics = false, bool includeMeals = false) =>
        new(
            IncludeStatistics: includeStatistics,
            IncludeMeals: includeMeals,
            IncludeWeight: false,
            IncludeWaist: false,
            IncludeHydration: false);
}
