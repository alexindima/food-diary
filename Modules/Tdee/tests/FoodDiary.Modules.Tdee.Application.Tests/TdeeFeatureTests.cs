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
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;
using FoodDiary.Modules.Tdee.Contracts.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Models;

namespace FoodDiary.Modules.Tdee.Application.Tests;

[ExcludeFromCodeCoverage]
public class TdeeFeatureTests {
    private static readonly DateTime Today = new(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData("unknown/zone", 2026, 9, 20)]
    [InlineData("UTC", 1, 1, 1)]
    [InlineData("UTC", 9999, 12, 31)]
    public async Task GetTdeeInsight_WithInvalidCalendar_ReturnsValidationBeforeReadingNutrition(string zoneId, int year, int month, int day) {
        var user = User.Create("invalid-calendar@example.com", "hash");
        IMealDailyCalorieReadService nutrition = CreateStatisticsReadService();
        GetTdeeInsightQueryHandler handler = CreateHandler(
            profileService: CreateProfileService(user), statisticsReadService: nutrition,
            currentUserAccessService: CreateCurrentUserAccessService(user));

        Result<TdeeInsightModel> result = await handler.Handle(
            new GetTdeeInsightQuery(user.Id.Value, new DateOnly(year, month, day), zoneId), CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Empty(nutrition.ReceivedCalls());
    }

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

    [Theory]
    [InlineData("Asia/Tbilisi", "2026-09-20")]
    [InlineData("Europe/Berlin", "2026-03-29")]
    [InlineData("Europe/Berlin", "2026-10-25")]
    [InlineData("Pacific/Kiritimati", "2026-01-01")]
    [InlineData("America/Los_Angeles", "2026-11-01")]
    public async Task GetTdeeInsight_AlignsNutritionInstantsWithCalendarSamples(string zoneId, string dateText) {
        var user = User.Create("local-tdee@example.com", "hash");
        var day = DateOnly.ParseExact(dateText, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(zoneId);
        var date = day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        DateTime from = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(-28).ToDateTime(TimeOnly.MinValue), zone);
        DateTime to = TimeZoneInfo.ConvertTimeToUtc(day.AddDays(1).ToDateTime(TimeOnly.MinValue), zone).AddTicks(-1);
        ISender sender = Substitute.For<ISender>();
        sender.Send(Arg.Any<ReadWeightEntriesQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models.WeightEntryModel>>([]));
        sender.Send(Arg.Any<ReadExerciseEntriesQuery>(), Arg.Any<CancellationToken>()).Returns(Task.FromResult<IReadOnlyList<FoodDiary.Modules.Exercises.Contracts.Models.ExerciseEntryModel>>([]));
        IMealDailyCalorieReadService nutrition = CreateStatisticsReadService();
        var handler = new GetTdeeInsightQueryHandler(CreateProfileService(user), sender, nutrition, new StubDateTimeProvider(), CreateCurrentUserAccessService(user));
        using var cancellation = new CancellationTokenSource();
        ResultAssert.Success(await handler.Handle(new GetTdeeInsightQuery(user.Id.Value, day, zoneId), cancellation.Token));
        await sender.Received(1).Send(Arg.Is<ReadWeightEntriesQuery>(q => q.DateFrom == date.AddDays(-28) && q.DateTo == date), cancellation.Token);
        await sender.Received(1).Send(Arg.Is<ReadExerciseEntriesQuery>(q => q.DateFrom == date.AddDays(-28) && q.DateTo == date), cancellation.Token);
        await nutrition.Received(1).GetDailyCaloriesAsync(user.Id, from, to, cancellation.Token, Arg.Is<TimeZoneInfo>(z => string.Equals(z.Id, zoneId, StringComparison.Ordinal)));
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
            .GetDailyCaloriesAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>(), Arg.Any<TimeZoneInfo>())
            .Returns(Task.FromResult(Result.Success<IReadOnlyList<MealDailyCalories>>([])));
        return service;
    }

    private static IMealDailyCalorieReadService CreateFailingStatisticsReadService(Error error) {
        IMealDailyCalorieReadService service = Substitute.For<IMealDailyCalorieReadService>();
        service
            .GetDailyCaloriesAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>(), Arg.Any<TimeZoneInfo>())
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
