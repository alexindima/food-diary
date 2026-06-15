using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Gamification.Queries.GetGamification;
using FoodDiary.Application.Abstractions.Meals.Common;
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
            CreateMealRepository(), CreateUserRepository(user: null), new StubDateTimeProvider());

        Result<GamificationModel> result = await handler.Handle(
            new GetGamificationQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetGamification_WhenUserNotFound_ReturnsFailure() {
        var handler = new GetGamificationQueryHandler(
            CreateMealRepository(), CreateUserRepository(user: null), new StubDateTimeProvider());

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

        var handler = new GetGamificationQueryHandler(mealRepo, CreateUserRepository(user), new StubDateTimeProvider());

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
            CreateMealRepository(), CreateUserRepository(user), new StubDateTimeProvider());

        Result<GamificationModel> result = await handler.Handle(
            new GetGamificationQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.CurrentStreak);
        Assert.Equal(0, result.Value.TotalMealsLogged);
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
                UserId id = call.Arg<UserId>();
                return Task.FromResult(user is not null && user.Id == id ? user : null);
            });
        return repository;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Today);
    }
}
