using FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals;
using FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics;
using FoodDiary.Testing;
using FoodDiary.Mediator;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Dashboard.Contracts.Models;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Services;
using FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;

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
        ISender statisticsReadService = Substitute.For<ISender>();
        statisticsReadService
            .Send(Arg.Any<ReadDashboardStatisticsQuery>(), Arg.Any<CancellationToken>())
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
        DateTime thisWeekStart = Today;
        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);
        ISender statisticsReadService = Substitute.For<ISender>();
        statisticsReadService
            .Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == userId && query.DateFrom == thisWeekStart && query.DateTo == Today && query.QuantizationDays == 1), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([])));
        statisticsReadService
            .Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == userId && query.DateFrom == lastWeekStart && query.DateTo == lastWeekEnd && query.QuantizationDays == 1), Arg.Any<CancellationToken>())
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
        DateTime thisWeekStart = Today;
        DateTime lastWeekStart = thisWeekStart.AddDays(-7);
        DateTime lastWeekEnd = thisWeekStart.AddDays(-1);
        DashboardStatisticsBucketReadModel[] thisWeekBuckets = [
            new(thisWeekStart, thisWeekStart, TotalCalories: 700, AverageProteins: 0, AverageFats: 0, AverageCarbs: 0, AverageFiber: 0, TotalProteins: 35, TotalFats: 20, TotalCarbs: 90),
            new(Today, Today, TotalCalories: 900, AverageProteins: 0, AverageFats: 0, AverageCarbs: 0, AverageFiber: 0, TotalProteins: 45, TotalFats: 30, TotalCarbs: 110),
        ];
        IMealActivityReadService mealActivityReadService = CreateMealActivityReadService();
        mealActivityReadService
            .GetCountAsync(
                userId,
                Arg.Is<MealQueryFilters>(filters => filters!.DateFrom == thisWeekStart && filters.DateTo == Today),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(3));
        ISender statisticsReadService = Substitute.For<ISender>();
        statisticsReadService
            .Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == userId && query.DateFrom == thisWeekStart && query.DateTo == Today && query.QuantizationDays == 1), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>(thisWeekBuckets)));
        statisticsReadService
            .Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == userId && query.DateFrom == lastWeekStart && query.DateTo == lastWeekEnd && query.QuantizationDays == 1), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([])));

        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            mealActivityReadService: mealActivityReadService,
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
    public async Task GetWeeklyCheckIn_WithHistoricalWeek_LoadsSelectedCalendarWeekAndPreviousWeek() {
        var userId = UserId.New();
        var user = User.Create("weekly-history@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        DateTime selectedWeekStart = Today.AddDays(-14);
        DateTime selectedWeekEnd = selectedWeekStart.AddDays(6);
        DateTime previousWeekStart = selectedWeekStart.AddDays(-7);
        DateTime previousWeekEnd = selectedWeekStart.AddDays(-1);
        ISender statisticsReadService = CreateStatisticsReadService();
        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            statisticsReadService: statisticsReadService,
            profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value, DateOnly.FromDateTime(selectedWeekStart)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(DateTimeKind.Utc, selectedWeekStart.Kind);
        await statisticsReadService.Received(1).Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == userId && query.DateFrom == selectedWeekStart && query.DateFrom.Kind == DateTimeKind.Utc && query.DateTo == selectedWeekEnd && query.DateTo.Kind == DateTimeKind.Utc && query.QuantizationDays == 1), Arg.Any<CancellationToken>());
        await statisticsReadService.Received(1).Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == userId && query.DateFrom == previousWeekStart && query.DateTo == previousWeekEnd && query.QuantizationDays == 1), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(2026, 4, 7)]
    [InlineData(2026, 4, 13)]
    public async Task GetWeeklyCheckIn_WithInvalidWeekStart_ReturnsValidationFailure(int year, int month, int day) {
        var userId = UserId.New();
        var user = User.Create("weekly-invalid-date@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        GetWeeklyCheckInQueryHandler handler = CreateHandler(profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value, new DateOnly(year, month, day)),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WithMinimumMonday_ReturnsValidationFailureWithoutReadingSummaries() {
        var userId = UserId.New();
        var user = User.Create("weekly-minimum-date@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        ISender statisticsReadService = CreateStatisticsReadService();
        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            statisticsReadService: statisticsReadService,
            profileService: CreateProfileService(user));

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value, DateOnly.MinValue),
            CancellationToken.None);

        ResultAssert.Failure(result, "Validation.Invalid");
        await statisticsReadService.DidNotReceiveWithAnyArgs().Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == default && query.DateFrom == default && query.DateTo == default && query.QuantizationDays == default), default);
    }

    [Fact]
    public async Task GetWeeklyCheckIn_WithEarliestSupportedMonday_LoadsBothWeeks() {
        var userId = UserId.New();
        var user = User.Create("weekly-earliest-date@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        ISender statisticsReadService = CreateStatisticsReadService();
        GetWeeklyCheckInQueryHandler handler = CreateHandler(
            statisticsReadService: statisticsReadService,
            profileService: CreateProfileService(user));
        var earliestSupportedMonday = new DateOnly(1, 1, 8);

        Result<WeeklyCheckInModel> result = await handler.Handle(
            new GetWeeklyCheckInQuery(userId.Value, earliestSupportedMonday),
            CancellationToken.None);

        ResultAssert.Success(result);
        await statisticsReadService.Received(1).Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == userId && query.DateFrom == new DateTime(1, 1, 8, 0, 0, 0, DateTimeKind.Utc) && query.DateTo == new DateTime(1, 1, 14, 0, 0, 0, DateTimeKind.Utc) && query.QuantizationDays == 1), Arg.Any<CancellationToken>());
        await statisticsReadService.Received(1).Send(Arg.Is<ReadDashboardStatisticsQuery>(query => query.UserId == userId && query.DateFrom == new DateTime(1, 1, 1, 0, 0, 0, DateTimeKind.Utc) && query.DateTo == new DateTime(1, 1, 7, 0, 0, 0, DateTimeKind.Utc) && query.QuantizationDays == 1), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task WeeklyCheckInUserProfileService_WithAccessibleUser_ReturnsDailyCalorieTarget() {
        var user = User.Create("weekly-profile@example.com", "hashed");
        user.UpdateGoals(dailyCalorieTarget: 2200);
        IUserWeeklyCheckInProfileReadService userProfileReadService = Substitute.For<IUserWeeklyCheckInProfileReadService>();
        userProfileReadService.GetWeeklyCheckInProfileAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Result.Success(new UserWeeklyCheckInProfileModel(user.DailyCalorieTarget)));
        var service = new WeeklyCheckInUserProfileService(CreateUserContextService(user), userProfileReadService);

        Result<WeeklyCheckInUserProfile> result = await service.GetAsync(user.Id, CancellationToken.None);

        WeeklyCheckInUserProfile profile = ResultAssert.Success(result);
        Assert.Equal(2200, profile.DailyCalorieTarget);
    }

    [Fact]
    public async Task WeeklyCheckInUserProfileService_WithMissingUser_ReturnsInvalidToken() {
        IUserWeeklyCheckInProfileReadService userProfileReadService = Substitute.For<IUserWeeklyCheckInProfileReadService>();
        userProfileReadService.GetWeeklyCheckInProfileAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<UserWeeklyCheckInProfileModel>(Errors.Authentication.InvalidToken));
        var service = new WeeklyCheckInUserProfileService(CreateUserContextService(user: null), userProfileReadService);

        Result<WeeklyCheckInUserProfile> result = await service.GetAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
    }

    [Fact]
    public async Task WeeklyCheckInUserProfileService_EnsureCanAccessAsync_ForwardsToAccessService() {
        var userId = UserId.New();
        ICurrentUserAccessService currentUserAccessService = Substitute.For<ICurrentUserAccessService>();
        currentUserAccessService
            .EnsureCanAccessAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<Error?>(Errors.Authentication.AccountDeleted));
        var service = new WeeklyCheckInUserProfileService(
            currentUserAccessService,
            Substitute.For<IUserWeeklyCheckInProfileReadService>());

        Error? error = await service.EnsureCanAccessAsync(userId, CancellationToken.None);

        Assert.Equal(Errors.Authentication.AccountDeleted, error);
    }

    private static GetWeeklyCheckInQueryHandler CreateHandler(
        IMealActivityReadService? mealActivityReadService = null,
        ISender? statisticsReadService = null,
        ISender? weightEntryReadService = null,
        ISender? waistEntryReadService = null,
        ISender? hydrationEntryReadService = null,
        IWeeklyCheckInUserProfileService? profileService = null) =>
        new(
            new WeeklyCheckInReadService(mealActivityReadService ?? CreateMealActivityReadService(), global::FoodDiary.Testing.RequestTestSender.Route((RequestTestSender.Route((statisticsReadService ?? CreateStatisticsReadService(), [typeof(ReadDashboardStatisticsQuery)]), (RequestTestSender.Route((weightEntryReadService ?? CreateWeightEntryReadService(), [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries.ReadWeightEntriesQuery)]), (waistEntryReadService ?? CreateWaistEntryReadService(), [typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries.ReadWaistEntriesQuery)])), [typeof(ReadWeightEntriesQuery), typeof(ReadWaistEntriesQuery)])), [typeof(global::FoodDiary.Modules.Dashboard.Contracts.Queries.ReadDashboardStatistics.ReadDashboardStatisticsQuery), typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries.ReadWeightEntriesQuery), typeof(global::FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Queries.ReadWaistEntries.ReadWaistEntriesQuery)]), (hydrationEntryReadService ?? CreateHydrationEntryReadService(), [typeof(global::FoodDiary.Modules.Hydration.Contracts.Queries.ReadHydrationDailyTotals.ReadHydrationDailyTotalsQuery)]))),
            profileService ?? CreateProfileService(user: null),
            new StubDateTimeProvider());

    private static IMealActivityReadService CreateMealActivityReadService() {
        IMealActivityReadService service = Substitute.For<IMealActivityReadService>();
        service
            .GetCountAsync(Arg.Any<UserId>(), Arg.Any<MealQueryFilters>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(0));
        return service;
    }

    private static ISender CreateStatisticsReadService(
        IReadOnlyList<DashboardStatisticsBucketReadModel>? buckets = null) {
        ISender service = Substitute.For<ISender>();
        service
            .Send(Arg.Any<ReadDashboardStatisticsQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>(buckets ?? [])));
        return service;
    }

    private static ISender CreateWeightEntryReadService() {
        ISender service = Substitute.For<ISender>();
        service.Send(Arg.Any<ReadWeightEntriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntryModel>>([]));
        return service;
    }

    private static ISender CreateWaistEntryReadService() {
        ISender service = Substitute.For<ISender>();
        service.Send(Arg.Any<ReadWaistEntriesQuery>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WaistEntryModel>>([]));
        return service;
    }

    private static ISender CreateHydrationEntryReadService() {
        ISender service = Substitute.For<ISender>();
        service.Send(Arg.Is<ReadHydrationDailyTotalsQuery>(q => true), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<(DateTime Date, int TotalMl)>>([]));
        return service;
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

}
