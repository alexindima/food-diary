using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.Hydration.Common;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.WaistEntries.Services;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.Tests.WeeklyCheckIn;

[ExcludeFromCodeCoverage]
public class WeeklyCheckInFeatureTests {
    private static readonly DateTime Today = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetWeeklyCheckIn_WithNullUserId_ReturnsFailure() {
        GetWeeklyCheckInQueryHandler handler = CreateHandler();

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WhenUserNotFound_ReturnsFailure() {
        GetWeeklyCheckInQueryHandler handler = CreateHandler();

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WithValidUser_ReturnsModel() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        GetWeeklyCheckInQueryHandler handler = CreateHandler(profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.NotNull(result.Value.ThisWeek);
        Assert.NotNull(result.Value.LastWeek);
        Assert.NotNull(result.Value.Trends);
        Assert.NotNull(result.Value.Suggestions);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WhenProfileLoadFailsAfterAccessCheck_ReturnsFailure() {
        var userId = UserId.New();
        IWeeklyCheckInUserProfileService profileService = Substitute.For<IWeeklyCheckInUserProfileService>();
        profileService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(null));
        profileService
            .GetAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<WeeklyCheckInUserProfile>(Errors.Authentication.InvalidToken)));
        GetWeeklyCheckInQueryHandler handler = CreateHandler(profileService: profileService);

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WhenCurrentWeekSummaryFails_ReturnsFailure() {
        var userId = UserId.New();
        var user = User.Create("weekly-current-summary-fails@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        statisticsReadService
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(
                Errors.Validation.Invalid("statistics", "Statistics unavailable."))));
        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            statisticsReadService: statisticsReadService,
            profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WhenLastWeekSummaryFails_ReturnsFailure() {
        var userId = UserId.New();
        var user = User.Create("weekly-last-summary-fails@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        DateTime thisWeekStart = Today.AddDays(-6);
        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        statisticsReadService
            .GetStatisticsAsync(userId, thisWeekStart, Today, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([])));
        statisticsReadService
            .GetStatisticsAsync(userId, lastWeekStart, lastWeekEnd, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IReadOnlyList<DashboardStatisticsBucketReadModel>>(
                Errors.Validation.Invalid("statistics", "Last week unavailable."))));
        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            statisticsReadService: statisticsReadService,
            profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WithNoData_ReturnsZeroSummaries() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        GetWeeklyCheckInQueryHandler handler = CreateHandler(profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.ThisWeek.TotalCalories);
        Assert.Equal(0, result.Value.ThisWeek.MealsLogged);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_UsesStatisticsBucketsAndMealCountForCurrentSummary() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        DateTime thisWeekStart = Today.AddDays(-6);
        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);
        DashboardStatisticsBucketReadModel[] thisWeekBuckets = [
            new(thisWeekStart, thisWeekStart, TotalCalories: 700, AverageProteins: 0, AverageFats: 0, AverageCarbs: 0, AverageFiber: 0, TotalProteins: 35, TotalFats: 20, TotalCarbs: 90),
            new(Today, Today, TotalCalories: 900, AverageProteins: 0, AverageFats: 0, AverageCarbs: 0, AverageFiber: 0, TotalProteins: 45, TotalFats: 30, TotalCarbs: 110),
        ];
        IMealRepository mealRepo = CreateMealRepository();
        mealRepo
            .GetCountAsync(
                userId,
                Arg.Is<MealQueryFilters>(filters => filters.DateFrom == thisWeekStart && filters.DateTo == Today),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(3));
        IDashboardStatisticsReadService statisticsReadService = Substitute.For<IDashboardStatisticsReadService>();
        statisticsReadService
            .GetStatisticsAsync(userId, thisWeekStart, Today, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>(thisWeekBuckets)));
        statisticsReadService
            .GetStatisticsAsync(userId, lastWeekStart, lastWeekEnd, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([])));

        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            mealRepo: mealRepo,
            statisticsReadService: statisticsReadService,
            profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value), CancellationToken.None);

        WeeklyCheckInModel model = ResultAssert.Success(result);
        Assert.Equal(1600, model.ThisWeek.TotalCalories);
        Assert.Equal(3, model.ThisWeek.MealsLogged);
        Assert.Equal(2, model.ThisWeek.DaysLogged);
        Assert.Equal(11.4, model.ThisWeek.AvgProteins);
        Assert.Equal(7.1, model.ThisWeek.AvgFats);
        Assert.Equal(28.6, model.ThisWeek.AvgCarbs);
    }

    [Fact]
    public async Task WeeklyCheckInUserProfileService_WithAccessibleUser_ReturnsDailyCalorieTarget() {
        var user = User.Create("weekly-profile@example.com", "hashed");
        user.UpdateGoals(dailyCalorieTarget: 2200);
        IUserContextService userContextService = CreateUserContextService(user);
        var service = new WeeklyCheckInUserProfileService(userContextService);

        Result<WeeklyCheckInUserProfile> result = await service.GetAsync(user.Id, CancellationToken.None);

        WeeklyCheckInUserProfile profile = ResultAssert.Success(result);
        Assert.Equal(2200, profile.DailyCalorieTarget);
    }

    [Fact]
    public async Task WeeklyCheckInUserProfileService_WithMissingUser_ReturnsInvalidToken() {
        IUserContextService userContextService = CreateUserContextService(user: null);
        var service = new WeeklyCheckInUserProfileService(userContextService);

        Result<WeeklyCheckInUserProfile> result = await service.GetAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
    }

    [Fact]
    public async Task WeeklyCheckInUserProfileService_EnsureCanAccessAsync_ForwardsToUserContextService() {
        var userId = UserId.New();
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(Errors.Authentication.AccountDeleted));
        var service = new WeeklyCheckInUserProfileService(userContextService);

        Error? error = await service.EnsureCanAccessAsync(userId, CancellationToken.None);

        Assert.Equal(Errors.Authentication.AccountDeleted, error);
    }

    private static GetWeeklyCheckInQueryHandler CreateHandler(
        IMealRepository? mealRepo = null,
        IDashboardStatisticsReadService? statisticsReadService = null,
        IWeightEntryRepository? weightRepo = null,
        IWaistEntryRepository? waistRepo = null,
        IHydrationEntryRepository? hydrationRepo = null,
        IWeeklyCheckInUserProfileService? profileService = null) =>
        new(
            new WeeklyCheckInReadService(
                mealRepo ?? CreateMealRepository(),
                statisticsReadService ?? CreateStatisticsReadService(),
                new WeightEntryReadService(weightRepo ?? CreateWeightEntryRepository()),
                new WaistEntryReadService(waistRepo ?? CreateWaistEntryRepository()),
                new HydrationReadServiceAdapter(hydrationRepo ?? CreateHydrationEntryRepository())),
            profileService ?? CreateProfileService(user: null),
            new StubDateTimeProvider());

    private static IMealRepository CreateMealRepository() {
        IMealRepository repository = Substitute.For<IMealRepository>();
        repository
            .GetCountAsync(Arg.Any<UserId>(), Arg.Any<MealQueryFilters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return repository;
    }

    private static IDashboardStatisticsReadService CreateStatisticsReadService(
        IReadOnlyList<DashboardStatisticsBucketReadModel>? buckets = null) {
        IDashboardStatisticsReadService service = Substitute.For<IDashboardStatisticsReadService>();
        service
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>(buckets ?? [])));
        return service;
    }

    private static IWeightEntryRepository CreateWeightEntryRepository() {
        IWeightEntryRepository repository = Substitute.For<IWeightEntryRepository>();
        repository
            .GetByPeriodAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntry>>([]));
        return repository;
    }

    private static IWaistEntryRepository CreateWaistEntryRepository() {
        IWaistEntryRepository repository = Substitute.For<IWaistEntryRepository>();
        repository
            .GetByPeriodAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WaistEntry>>([]));
        return repository;
    }

    private static IHydrationEntryRepository CreateHydrationEntryRepository() {
        IHydrationEntryRepository repository = Substitute.For<IHydrationEntryRepository>();
        repository
            .GetDailyTotalsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<(DateTime Date, int TotalMl)>>([]));
        return repository;
    }

    private static IUserContextService CreateUserContextService(User? user) {
        IUserContextService service = Substitute.For<IUserContextService>();
        service
            .GetAccessibleUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                return Task.FromResult(user is not null && user.Id == userId
                    ? Result.Success(user)
                    : Result.Failure<User>(Errors.Authentication.InvalidToken));
            });
        return service;
    }

    private static IWeeklyCheckInUserProfileService CreateProfileService(User? user) {
        IWeeklyCheckInUserProfileService service = Substitute.For<IWeeklyCheckInUserProfileService>();
        service
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                if (user is null || user.Id != id) {
                    return Task.FromResult(Result.Failure<WeeklyCheckInUserProfile>(Errors.Authentication.InvalidToken));
                }

                if (user.DeletedAt is not null) {
                    return Task.FromResult(Result.Failure<WeeklyCheckInUserProfile>(Errors.Authentication.AccountDeleted));
                }

                return Task.FromResult(Result.Success(new WeeklyCheckInUserProfile(user.DailyCalorieTarget)));
            });
        return service;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Today);
    }

    [ExcludeFromCodeCoverage]
    private sealed class HydrationReadServiceAdapter(IHydrationEntryReadModelRepository repository) : IHydrationEntryReadService {
        public Task<IReadOnlyList<HydrationEntryModel>> GetEntriesByDateAsync(
            UserId userId,
            DateTime dateUtc,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<HydrationEntryModel>>([]);

        public Task<int> GetDailyTotalAsync(
            UserId userId,
            DateTime dateUtc,
            CancellationToken cancellationToken) =>
            repository.GetDailyTotalAsync(userId, dateUtc, cancellationToken);

        public Task<IReadOnlyList<(DateTime Date, int TotalMl)>> GetDailyTotalsAsync(
            UserId userId,
            DateTime dateFrom,
            DateTime dateTo,
            CancellationToken cancellationToken) =>
            repository.GetDailyTotalsAsync(userId, dateFrom, dateTo, cancellationToken);
    }
}
