using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Infrastructure.Persistence.Dashboard;

namespace FoodDiary.Infrastructure.Tests.Persistence;

[ExcludeFromCodeCoverage]
public sealed class DashboardReadServiceTests {
    [Fact]
    public async Task GetSnapshotDataAsync_WhenStatisticsFails_ReturnsFailure() {
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
            DateTime.UtcNow.Date.AddDays(1),
            DateTime.UtcNow.Date,
            periodDays: 2,
            page: 1,
            pageSize: 10,
            Sections(includeStatistics: true),
            CancellationToken.None);

        Assert.Equal(error, result.Error);
    }

    [Fact]
    public async Task GetSnapshotDataAsync_ForSingleDay_ReusesWeeklyStatisticsQuery() {
        var userId = UserId.New();
        var dayStart = new DateTime(2026, 8, 29, 0, 0, 0, DateTimeKind.Utc);
        DateTime dayEnd = dayStart.AddDays(1).AddTicks(-1);
        DateTime weeklyFrom = dayStart.AddDays(-6);
        var previousBucket = new DashboardStatisticsBucketReadModel(
            weeklyFrom,
            weeklyFrom.AddDays(1).AddTicks(-1),
            TotalCalories: 1200,
            AverageProteins: 80,
            AverageFats: 50,
            AverageCarbs: 140,
            AverageFiber: 20);
        var currentBucket = new DashboardStatisticsBucketReadModel(
            dayStart,
            dayEnd,
            TotalCalories: 1800,
            AverageProteins: 100,
            AverageFats: 60,
            AverageCarbs: 200,
            AverageFiber: 25);
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        statisticsReadService
            .GetStatisticsAsync(userId, weeklyFrom, dayEnd, 1, CancellationToken.None)
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([
                previousBucket,
                currentBucket,
            ])));
        DashboardReadService service = CreateService(statisticsReadService);

        Result<DashboardReadModel> result = await service.GetSnapshotDataAsync(
            userId,
            dayStart,
            dayEnd,
            weeklyFrom,
            periodDays: 1,
            page: 1,
            pageSize: 10,
            Sections(includeStatistics: true),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.Message);
        DashboardReadModel model = result.Value;
        Assert.Same(currentBucket, Assert.Single(model.Statistics));
        Assert.Equal([previousBucket, currentBucket], model.WeeklyStatistics);
        Assert.Single(statisticsReadService.ReceivedCalls());
    }

    [Fact]
    public async Task GetSnapshotDataAsync_WithMinimumDate_ReturnsValidationFailureInsteadOfOverflowing() {
        DashboardReadService service = CreateService();

        Result<DashboardReadModel> result = await service.GetSnapshotDataAsync(
            UserId.New(),
            DateTime.MinValue,
            DateTime.MinValue,
            DateTime.MinValue,
            periodDays: 1,
            page: 1,
            pageSize: 10,
            Sections(),
            CancellationToken.None);

        Assert.Multiple(
            () => Assert.True(result.IsFailure),
            () => Assert.Equal("Validation.Invalid", result.Error.Code),
            () => Assert.Contains("weekly range", result.Error.Message, StringComparison.OrdinalIgnoreCase));
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

    [Fact]
    public async Task GetSnapshotDataAsync_PreservesExactDayBoundariesForBodyData() {
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
        DashboardReadService service = CreateService(bodyReadService: bodyReadService);
        var userId = UserId.New();
        var dayStart = new DateTime(2026, 8, 4, 20, 0, 0, DateTimeKind.Utc);
        DateTime dayEnd = dayStart.AddDays(1).AddTicks(-1);

        Result<DashboardReadModel> result = await service.GetSnapshotDataAsync(
            userId,
            dayStart,
            dayEnd,
            dayStart.AddDays(-6),
            periodDays: 1,
            page: 1,
            pageSize: 10,
            Sections(),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        await bodyReadService.Received(1).GetBodyAsync(
            userId,
            dayStart,
            dayEnd,
            dayStart.AddDays(-6),
            trendQuantizationDays: 1,
            includeWeight: false,
            includeWaist: false,
            includeHydration: false,
            CancellationToken.None);
    }

    private static DashboardReadService CreateService(
        IDashboardStatisticsReadService? statisticsReadService = null,
        IDashboardMealsReadService? mealsReadService = null,
        IDashboardBodyReadService? bodyReadService = null) {
        IDashboardStatisticsReadService resolvedStatisticsReadService = statisticsReadService ?? CreateSuccessfulStatisticsReadService();
        IDashboardBodyReadService resolvedBodyReadService = bodyReadService ?? Substitute.For<IDashboardBodyReadService>();
        resolvedBodyReadService
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
            resolvedBodyReadService,
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
