using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Tdee.Models;

namespace FoodDiary.Application.Tests.Tdee;

[ExcludeFromCodeCoverage]
public class TdeeFeatureTests {
    private static readonly DateTime Today = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetTdeeInsight_WithNullUserId_ReturnsFailure() {
        GetTdeeInsightQueryHandler handler = CreateHandler();

        Result<TdeeInsightModel> result = await handler.Handle(
            new GetTdeeInsightQuery(UserId: null), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetTdeeInsight_WhenUserNotFound_ReturnsFailure() {
        GetTdeeInsightQueryHandler handler = CreateHandler();

        Result<TdeeInsightModel> result = await handler.Handle(
            new GetTdeeInsightQuery(Guid.NewGuid()), CancellationToken.None);

        ResultAssert.Failure(result);
    }

    [Fact]
    public async Task GetTdeeInsight_WhenUserDisappearsAfterAccessCheck_ReturnsNotFound() {
        var userId = UserId.New();
        var user = User.Create("disappearing-tdee-user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        GetTdeeInsightQueryHandler handler = CreateHandler(userRepo: CreateDisappearingUserRepository(user));

        Result<TdeeInsightModel> result = await handler.Handle(
            new GetTdeeInsightQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("User.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task GetTdeeInsight_WithValidUser_ReturnsModel() {
        var userId = UserId.New();
        var user = User.Create("user@test.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);

        GetTdeeInsightQueryHandler handler = CreateHandler(userRepo: CreateUserRepository(user));

        Result<TdeeInsightModel> result = await handler.Handle(
            new GetTdeeInsightQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.DataDaysUsed);
    }

    private static GetTdeeInsightQueryHandler CreateHandler(
        IUserRepository? userRepo = null) =>
        new(
            userRepo ?? CreateUserRepository(user: null),
            CreateWeightEntryRepository(),
            CreateMealRepository(),
            CreateExerciseEntryRepository(),
            new StubDateTimeProvider());

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

    private static IUserRepository CreateDisappearingUserRepository(User user) {
        IUserRepository repository = Substitute.For<IUserRepository>();
        repository
            .GetByIdAsync(user.Id, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<User?>(user), Task.FromResult<User?>(null));
        return repository;
    }

    private static IWeightEntryRepository CreateWeightEntryRepository() {
        IWeightEntryRepository repository = Substitute.For<IWeightEntryRepository>();
        repository
            .GetByPeriodAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntry>>([]));
        return repository;
    }

    private static IMealRepository CreateMealRepository() {
        IMealRepository repository = Substitute.For<IMealRepository>();
        repository
            .GetByPeriodAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Meal>>([]));
        return repository;
    }

    private static IExerciseEntryRepository CreateExerciseEntryRepository() {
        IExerciseEntryRepository repository = Substitute.For<IExerciseEntryRepository>();
        repository
            .GetByDateRangeAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ExerciseEntry>>([]));
        return repository;
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Today);
    }
}
