using FoodDiary.Modules.Meals.Application.Queries.ReadDistinctMealDates;
using FoodDiary.Modules.Meals.Application.Queries.ReadTotalMealCount;
using FoodDiary.Testing;
using FoodDiary.Modules.Meals.Application.Queries.ReadMealCount;
using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Gamification.Application.Queries.GetGamification;
using FoodDiary.Modules.Gamification.Application.Common;
using FoodDiary.Modules.Meals.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Gamification.Application.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;
using FoodDiary.Modules.Gamification.Application.Abstractions.Achievements.Common;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Modules.Gamification.Application.Tests;

[ExcludeFromCodeCoverage]
public class GamificationFeatureTests {
    private static readonly DateTime Today = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetGamification_WithNullUserId_ReturnsFailure() {
        GetGamificationQueryHandler handler = CreateGamificationHandler(
            CreateMealRepository(), CreateStatisticsReadService(), CreateUserProfileService(user: null), CreateCurrentUserAccessService(user: null));

        Result<GamificationModel> result = await handler.Handle(
            new GetGamificationQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetGamification_WhenUserNotFound_ReturnsFailure() {
        GetGamificationQueryHandler handler = CreateGamificationHandler(
            CreateMealRepository(), CreateStatisticsReadService(), CreateUserProfileService(user: null), CreateCurrentUserAccessService(user: null));

        Result<GamificationModel> result = await handler.Handle(
            new GetGamificationQuery(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetGamification_WithValidUser_ReturnsModel() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        IMealRepository mealRepo = CreateMealRepository([Today, Today.AddDays(-1), Today.AddDays(-2)], totalMealCount: 15);

        GetGamificationQueryHandler handler = CreateGamificationHandler(
            mealRepo, CreateStatisticsReadService(), CreateUserProfileService(user), CreateCurrentUserAccessService(user));

        Result<GamificationModel> result = await handler.Handle(
            new GetGamificationQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(3, result.Value.CurrentStreak);
        Assert.Equal(15, result.Value.TotalMealsLogged);
    }

    [Fact]
    public async Task GetGamification_WithNoMeals_ReturnsZeroStreaks() {
        var userId = UserId.New();
        var user = User.Create("user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        GetGamificationQueryHandler handler = CreateGamificationHandler(
            CreateMealRepository(), CreateStatisticsReadService(), CreateUserProfileService(user), CreateCurrentUserAccessService(user));

        Result<GamificationModel> result = await handler.Handle(
            new GetGamificationQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.CurrentStreak);
        Assert.Equal(0, result.Value.TotalMealsLogged);
    }

    [Fact]
    public async Task GetGamificationQueryHandler_IgnoresNonPositiveDailyCaloriesBuckets() {
        var userId = UserId.New();
        var user = User.Create("gamification-calories@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        user.UpdateGoals(dailyCalorieTarget: 2000);
        IReadOnlyList<MealNutritionStatisticsBucket> buckets = [
            CreateStatisticsBucket(Today.AddDays(-2), totalCalories: 1800),
            CreateStatisticsBucket(Today.AddDays(-1), totalCalories: 0),
            CreateStatisticsBucket(Today, totalCalories: -100),
        ];
        GetGamificationQueryHandler service = CreateGamificationHandler(
            CreateMealRepository([Today], totalMealCount: 1),
            CreateStatisticsReadService(buckets),
            CreateUserProfileService(user));

        Result<GamificationModel> result = await service.Handle(new GetGamificationQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.InRange(result.Value.WeeklyAdherence, 0.14, 0.15);
    }

    [Fact]
    public async Task GetGamificationQueryHandler_WhenProfileFails_ReturnsFailure() {
        var userId = UserId.New();
        IUserGamificationProfileReadService userProfileService = Substitute.For<IUserGamificationProfileReadService>();
        userProfileService
            .GetGamificationProfileAsync(userId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<UserGamificationProfileModel>(AuthenticationErrors.InvalidToken)));
        GetGamificationQueryHandler service = CreateGamificationHandler(
            CreateMealRepository(),
            CreateStatisticsReadService(),
            userProfileService);

        Result<GamificationModel> result = await service.Handle(new GetGamificationQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetGamificationQueryHandler_WhenStatisticsFail_ReturnsFailure() {
        var userId = UserId.New();
        var user = User.Create("gamification-statistics-failure@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        IMealNutritionStatisticsReadService statisticsReadService = Substitute.For<IMealNutritionStatisticsReadService>();
        statisticsReadService
            .GetStatisticsAsync(userId, Today.AddDays(-6), Today, 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IReadOnlyList<MealNutritionStatisticsBucket>>(
                Errors.Validation.Invalid("statistics", "Statistics unavailable."))));
        GetGamificationQueryHandler service = CreateGamificationHandler(
            CreateMealRepository(),
            statisticsReadService,
            CreateUserProfileService(user));

        Result<GamificationModel> result = await service.Handle(new GetGamificationQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GamificationUserProfileService_WithAccessibleUser_ReturnsProfile() {
        var user = User.Create("profile@example.com", "hashed");
        user.UpdateGoals(dailyCalorieTarget: 2100);
        IUserGamificationProfileReadService profileReadService = CreateGamificationProfileReadService(user);
        GetGamificationQueryHandler service = CreateGamificationHandler(CreateMealRepository(), CreateStatisticsReadService([CreateStatisticsBucket(Today, 2100)]), profileReadService);

        Result<GamificationModel> result = await service.Handle(new GetGamificationQuery(user.Id.Value), CancellationToken.None);

        GamificationModel profile = ResultAssert.Success(result);
        Assert.InRange(profile.WeeklyAdherence, 0.14, 0.15);
    }

    [Fact]
    public async Task GamificationUserProfileService_WithMissingUser_ReturnsInvalidToken() {
        IUserGamificationProfileReadService profileReadService = CreateGamificationProfileReadService(user: null);
        GetGamificationQueryHandler service = CreateGamificationHandler(CreateMealRepository(), CreateStatisticsReadService([CreateStatisticsBucket(Today, 2100)]), profileReadService);

        Result<GamificationModel> result = await service.Handle(new GetGamificationQuery(UserId.New().Value), CancellationToken.None);

        ResultAssert.Failure(result, "Authentication.InvalidToken");
    }

    private static IMealRepository CreateMealRepository(
        IReadOnlyList<DateTime>? distinctDates = null,
        int totalMealCount = 0) {
        IMealRepository repository = Substitute.For<IMealRepository>();
        repository
            .GetDistinctMealDatesAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(distinctDates ?? []));
        repository
            .GetTotalMealCountAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(totalMealCount));
        return repository;
    }

    private static GetGamificationQueryHandler CreateGamificationHandler(
        IMealActivityReadRepository mealRepository,
        IMealNutritionStatisticsReadService statisticsReadService,
        IUserGamificationProfileReadService userProfileService, ICurrentUserAccessService? access = null) =>
        new(
            RequestTestSender.Create(new ReadMealCountQueryHandler(mealRepository), new ReadDistinctMealDatesQueryHandler(mealRepository), new ReadTotalMealCountQueryHandler(mealRepository)),
            statisticsReadService,
            userProfileService,
            CreateAchievementMetricReader(),
            CreateAchievementAwardService(),
            new StubDateTimeProvider(), access ?? Substitute.For<ICurrentUserAccessService>());

    private static IAchievementAwardService CreateAchievementAwardService() {
        IAchievementAwardService service = Substitute.For<IAchievementAwardService>();
        service
            .EvaluateAndGrantAsync(
                Arg.Any<UserId>(),
                Arg.Any<AchievementMetricSnapshot>(),
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<BadgeModel>>([]));
        return service;
    }

    private static IAchievementMetricReader CreateAchievementMetricReader() {
        IAchievementMetricReader reader = Substitute.For<IAchievementMetricReader>();
        reader.GetCompletedAcademyArticleCountAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>()).Returns(0);
        return reader;
    }

    private static IMealNutritionStatisticsReadService CreateStatisticsReadService(
        IReadOnlyList<MealNutritionStatisticsBucket>? buckets = null) {
        IMealNutritionStatisticsReadService service = Substitute.For<IMealNutritionStatisticsReadService>();
        service
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<MealNutritionStatisticsBucket>>(buckets ?? [])));
        return service;
    }

    private static MealNutritionStatisticsBucket CreateStatisticsBucket(DateTime date, double totalCalories) =>
        new(date, date.AddDays(1), totalCalories, AverageProteins: 0, AverageFats: 0, AverageCarbs: 0, AverageFiber: 0);

    private static IUserGamificationProfileReadService CreateGamificationProfileReadService(User? user) {
        IUserGamificationProfileReadService service = Substitute.For<IUserGamificationProfileReadService>();
        service
            .GetGamificationProfileAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                return Task.FromResult(user is not null && user.Id == userId
                    ? Result.Success(new UserGamificationProfileModel(new UserCalorieSchedule(
                        user.DailyCalorieTarget, user.CalorieCyclingEnabled,
                        user.MondayCalories, user.TuesdayCalories, user.WednesdayCalories,
                        user.ThursdayCalories, user.FridayCalories, user.SaturdayCalories, user.SundayCalories)))
                    : Result.Failure<UserGamificationProfileModel>(AuthenticationErrors.InvalidToken));
            });
        return service;
    }

    private static IUserGamificationProfileReadService CreateUserProfileService(User? user) =>
        CreateGamificationProfileReadService(user);

    private static ICurrentUserAccessService CreateCurrentUserAccessService(User? user) {
        ICurrentUserAccessService service = Substitute.For<ICurrentUserAccessService>();
        service
            .EnsureCanAccessAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                if (user is null || user.Id != id) {
                    return Task.FromResult<Error?>(AuthenticationErrors.InvalidToken);
                }

                return Task.FromResult(user.DeletedAt is null ? null : UserAuthenticationErrors.AccountDeleted);
            });
        return service;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Today);
    }
}
