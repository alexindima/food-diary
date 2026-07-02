using FoodDiary.Application.Abstractions.Hydration.Common;
using FoodDiary.Application.Abstractions.Meals.Common;
using FoodDiary.Application.Abstractions.WaistEntries.Common;
using FoodDiary.Application.WeeklyCheckIn.Common;
using FoodDiary.Application.WeeklyCheckIn.Queries.GetWeeklyCheckIn;
using FoodDiary.Application.Abstractions.WeightEntries.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.WeeklyCheckIn.Models;

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

    private static GetWeeklyCheckInQueryHandler CreateHandler(
        IMealRepository? mealRepo = null,
        IWeightEntryRepository? weightRepo = null,
        IWaistEntryRepository? waistRepo = null,
        IHydrationEntryRepository? hydrationRepo = null,
        IWeeklyCheckInUserProfileService? profileService = null) =>
        new(
            mealRepo ?? CreateMealRepository(),
            weightRepo ?? CreateWeightEntryRepository(),
            waistRepo ?? CreateWaistEntryRepository(),
            hydrationRepo ?? CreateHydrationEntryRepository(),
            profileService ?? CreateProfileService(user: null),
            new StubDateTimeProvider());

    private static IMealRepository CreateMealRepository() {
        IMealRepository repository = Substitute.For<IMealRepository>();
        repository
            .GetByPeriodAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<Meal>>([]));
        return repository;
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
