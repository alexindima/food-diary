using FoodDiary.Application.Abstractions.Authentication.Common;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Mediator;
using FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseCalories;
using FoodDiary.Modules.Exercises.Application.Queries.ReadExerciseEntries;
using FoodDiary.Modules.Exercises.Contracts.Queries.ReadExerciseEntries;
using FoodDiary.Modules.Exercises.Domain.Entities.Tracking;
using FoodDiary.Testing;
using FoodDiary.Modules.BodyMetrics.Application.WeightEntries.Queries.ReadWeightEntries;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Modules.Exercises.Application.Abstractions.Common;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Tdee.Application.Queries.GetTdeeInsight;
using FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;
using FoodDiary.Modules.BodyMetrics.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Tdee.Contracts.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Abstractions.Users.Models;

namespace FoodDiary.Modules.Tdee.Application.Tests;

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
        GetTdeeInsightQueryHandler handler = CreateHandler(
            profileService: CreateFailingProfileService(UserErrors.NotFound()),
            currentUserAccessService: CreateCurrentUserAccessService(user));

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

        GetTdeeInsightQueryHandler handler = CreateHandler(
            profileService: CreateProfileService(user),
            currentUserAccessService: CreateCurrentUserAccessService(user));

        Result<TdeeInsightModel> result = await handler.Handle(
            new GetTdeeInsightQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.DataDaysUsed);
    }

    [Fact]
    public async Task GetTdeeInsight_WhenStatisticsReadFails_ReturnsFailure() {
        var userId = UserId.New();
        var user = User.Create("tdee-statistics-fail@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        GetTdeeInsightQueryHandler handler = CreateHandler(
            profileService: CreateProfileService(user),
            statisticsReadService: CreateFailingStatisticsReadService(Errors.Validation.Invalid("statistics", "Statistics unavailable.")),
            currentUserAccessService: CreateCurrentUserAccessService(user));

        Result<TdeeInsightModel> result = await handler.Handle(
            new GetTdeeInsightQuery(userId.Value), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    private static GetTdeeInsightQueryHandler CreateHandler(
        IUserTdeeProfileReadService? profileService = null,
        IMealDailyCalorieReadService? statisticsReadService = null,
        ICurrentUserAccessService? currentUserAccessService = null) =>
        new(
            profileService ?? CreateProfileService(user: null),
            RequestTestSender.Route(
                (RequestTestSender.Create(new ReadWeightEntriesQueryHandler(CreateWeightEntryRepository())), [typeof(ReadWeightEntriesQuery)]),
                (CreateExerciseEntryReadService(), [typeof(ReadExerciseEntriesQuery)])),
            statisticsReadService ?? CreateStatisticsReadService(),
            new StubDateTimeProvider(),
            currentUserAccessService ?? CreateCurrentUserAccessService(user: null));

    private static IUserTdeeProfileReadService CreateProfileService(User? user) {
        IUserTdeeProfileReadService service = Substitute.For<IUserTdeeProfileReadService>();
        service
            .GetTdeeProfileAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                if (user is null || user.Id != id) {
                    return Task.FromResult(Result.Failure<UserTdeeProfileModel>(AuthenticationErrors.InvalidToken));
                }

                if (user.DeletedAt is not null) {
                    return Task.FromResult(Result.Failure<UserTdeeProfileModel>(UserAuthenticationErrors.AccountDeleted));
                }

                return Task.FromResult(Result.Success(new UserTdeeProfileModel(
                    user.CalculateBmr(),
                    user.CalculateEstimatedTdee(),
                    user.WeightKg,
                    user.DesiredWeightKg,
                    user.DailyCalorieTarget)));
            });
        return service;
    }

    private static IUserTdeeProfileReadService CreateFailingProfileService(Error error) {
        IUserTdeeProfileReadService service = Substitute.For<IUserTdeeProfileReadService>();
        service
            .GetTdeeProfileAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<UserTdeeProfileModel>(error)));
        return service;
    }

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

    private static IWeightEntryReadModelRepository CreateWeightEntryRepository() {
        IWeightEntryReadModelRepository repository = Substitute.For<IWeightEntryReadModelRepository>();
        repository
            .GetByPeriodReadModelsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntryReadModel>>([]));
        return repository;
    }

    private static IMealDailyCalorieReadService CreateStatisticsReadService() {
        IMealDailyCalorieReadService service = Substitute.For<IMealDailyCalorieReadService>();
        service
            .GetDailyCaloriesAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<MealDailyCalories>>([])));
        return service;
    }

    private static IMealDailyCalorieReadService CreateFailingStatisticsReadService(Error error) {
        IMealDailyCalorieReadService service = Substitute.For<IMealDailyCalorieReadService>();
        service
            .GetDailyCaloriesAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<IReadOnlyList<MealDailyCalories>>(error)));
        return service;
    }

    private static IExerciseEntryRepository CreateExerciseEntryRepository() {
        IExerciseEntryRepository repository = Substitute.For<IExerciseEntryRepository>();
        repository
            .GetByDateRangeAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<ExerciseEntry>>([]));
        return repository;
    }

    private static ISender CreateExerciseEntryReadService() {
        IExerciseEntryRepository repository = CreateExerciseEntryRepository();
        return RequestTestSender.Create(new ReadExerciseEntriesQueryHandler(repository), new ReadExerciseCaloriesQueryHandler(repository));
    }

    [ExcludeFromCodeCoverage]
    private sealed class StubDateTimeProvider : TimeProvider {
        public override DateTimeOffset GetUtcNow() => new(Today);
    }
}
