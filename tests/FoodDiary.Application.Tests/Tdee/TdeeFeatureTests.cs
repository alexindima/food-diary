using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Exercises.Common;
using FoodDiary.Application.Abstractions.Dashboard.Common;
using FoodDiary.Application.Abstractions.Dashboard.Models;
using FoodDiary.Application.Exercises.Services;
using FoodDiary.Application.Tdee.Common;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Application.WeightEntries.Services;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Services;
using FoodDiary.Application.Users.Common;

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
    public async Task TdeeUserProfileService_WhenUserMissing_ReturnsAccessFailure() {
        IUserContextService userContextService = Substitute.For<IUserContextService>();
        userContextService
            .GetAccessibleUserAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Result.Failure<User>(Errors.Authentication.InvalidToken));
        var service = new TdeeUserProfileService(userContextService);

        Result<TdeeUserProfile> result = await service.GetAsync(UserId.New(), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetTdeeInsight_WhenUserDisappearsAfterAccessCheck_ReturnsNotFound() {
        var userId = UserId.New();
        var user = User.Create("disappearing-tdee-user@example.com", "hashed");
        typeof(User).GetProperty(nameof(User.Id))!.SetValue(user, userId);
        GetTdeeInsightQueryHandler handler = CreateHandler(profileService: CreateFailingProfileService(Errors.User.NotFound()));

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

        GetTdeeInsightQueryHandler handler = CreateHandler(profileService: CreateProfileService(user));

        Result<TdeeInsightModel> result = await handler.Handle(
            new GetTdeeInsightQuery(userId.Value), CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Equal(0, result.Value.DataDaysUsed);
    }

    private static GetTdeeInsightQueryHandler CreateHandler(
        ITdeeUserProfileService? profileService = null) =>
        new(
            profileService ?? CreateProfileService(user: null),
            new WeightEntryReadService(CreateWeightEntryRepository()),
            CreateStatisticsReadService(),
            new ExerciseEntryReadService(CreateExerciseEntryRepository()),
            new StubDateTimeProvider());

    private static ITdeeUserProfileService CreateProfileService(User? user) {
        ITdeeUserProfileService service = Substitute.For<ITdeeUserProfileService>();
        service
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(call => {
                UserId id = call.Arg<UserId>();
                if (user is null || user.Id != id) {
                    return Task.FromResult(Result.Failure<TdeeUserProfile>(Errors.Authentication.InvalidToken));
                }

                if (user.DeletedAt is not null) {
                    return Task.FromResult(Result.Failure<TdeeUserProfile>(Errors.Authentication.AccountDeleted));
                }

                return Task.FromResult(Result.Success(new TdeeUserProfile(
                    user.CalculateBmr(),
                    user.CalculateEstimatedTdee(),
                    user.Weight,
                    user.DesiredWeight,
                    user.DailyCalorieTarget)));
            });
        return service;
    }

    private static ITdeeUserProfileService CreateFailingProfileService(Error error) {
        ITdeeUserProfileService service = Substitute.For<ITdeeUserProfileService>();
        service
            .GetAsync(Arg.Any<UserId>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure<TdeeUserProfile>(error)));
        return service;
    }

    private static IWeightEntryRepository CreateWeightEntryRepository() {
        IWeightEntryRepository repository = Substitute.For<IWeightEntryRepository>();
        repository
            .GetByPeriodAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<WeightEntry>>([]));
        return repository;
    }

    private static IDashboardStatisticsReadService CreateStatisticsReadService() {
        IDashboardStatisticsReadService service = Substitute.For<IDashboardStatisticsReadService>();
        service
            .GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<DashboardStatisticsBucketReadModel>>([])));
        return service;
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
