using FoodDiary.Application.Gamification.Queries.GetGamification;
using FoodDiary.Application.Gamification.Common;
using FoodDiary.Application.Gamification.Services;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Gamification.Models;

namespace FoodDiary.Application.Tests.Gamification;

[ExcludeFromCodeCoverage]
public class GamificationFeatureTests {
    private static readonly DateTime Today = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetGamification_WithNullUserId_ReturnsFailure() {
        var handler = new GetGamificationQueryHandler(
            CreateMealRepository(), CreateUserProfileService(user: null), new StubDateTimeProvider());

        Result<GamificationModel> result = await handler.Handle(
            new GetGamificationQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetGamification_WhenUserNotFound_ReturnsFailure() {
        var handler = new GetGamificationQueryHandler(
            CreateMealRepository(), CreateUserProfileService(user: null), new StubDateTimeProvider());

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

        var handler = new GetGamificationQueryHandler(mealRepo, CreateUserProfileService(user), new StubDateTimeProvider());

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

        var handler = new GetGamificationQueryHandler(
            CreateMealRepository(), CreateUserProfileService(user), new StubDateTimeProvider());

        Result<GamificationModel> result = await handler.Handle(
            new GetGamificationQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.CurrentStreak);
        Assert.Equal(0, result.Value.TotalMealsLogged);
    }

    [Fact]
    public async Task GamificationUserProfileService_WithAccessibleUser_ReturnsProfile() {
        var user = User.Create("profile@example.com", "hashed");
        user.UpdateGoals(dailyCalorieTarget: 2100);
        IUserRepository repository = CreateUserRepository(user);
        var service = new GamificationUserProfileService(repository);

        Result<IGamificationUserProfile> result = await service.GetAsync(user.Id, CancellationToken.None);

        IGamificationUserProfile profile = ResultAssert.Success(result);
        Assert.Equal(2100, profile.GetCalorieTargetForDate(Today));
    }

    [Fact]
    public async Task GamificationUserProfileService_WithMissingUser_ReturnsInvalidToken() {
        IUserRepository repository = CreateUserRepository(user: null);
        var service = new GamificationUserProfileService(repository);

        Result<IGamificationUserProfile> result = await service.GetAsync(UserId.New(), CancellationToken.None);

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
        repository
            .GetByPeriodAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Meal>>([]));
        return repository;
    }

    private static IUserRepository CreateUserRepository(User? user) {
        IUserRepository repository = Substitute.For<IUserRepository>();
        repository
            .GetByIdAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId userId = call.Arg<UserId>();
                return Task.FromResult(user is not null && user.Id == userId ? user : null);
            });
        return repository;
    }

    private static IGamificationUserProfileService CreateUserProfileService(User? user) {
        IGamificationUserProfileService service = Substitute.For<IGamificationUserProfileService>();
        service
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user is not null && user.Id == id
                    ? Result.Success<IGamificationUserProfile>(new StubGamificationUserProfile(user))
                    : Result.Failure<IGamificationUserProfile>(Errors.Authentication.InvalidToken));
            });
        return service;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubGamificationUserProfile(User user) : IGamificationUserProfile {
        public double? GetCalorieTargetForDate(DateTime date) => user.GetCalorieTargetForDate(date);
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Today);
    }
}
