using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Results;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Cycles.Queries.GetCycleNutritionSummary;
using FoodDiary.Domain.Entities.Tracking;
using FoodDiary.Domain.Entities.Users;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Application.Tests.Cycles;

public partial class CyclesFeatureTests {

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithCycleLogsAndMeals_ReturnsBleedingComparison() {
        var user = User.Create("cycle-nutrition@example.com", "hash");
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.UpsertBleedingEntry(startDate, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 8, notes: null);
        profile.UpsertSymptomEntry(startDate.AddDays(1), CycleSymptomCategory.Craving, 6, ["sweet"], note: null);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([
                CreateNutritionBucket(startDate, calories: 2100, fiber: 18),
                CreateNutritionBucket(startDate.AddDays(1), calories: 1800, fiber: 28),
            ]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(2)),
            CancellationToken.None);

        ResultAssert.Success(result);
        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(result.Value);
        Assert.Equal(2, summary.LoggedCycleDays);
        Assert.Equal(2, summary.DaysWithMeals);
        Assert.Equal(1, summary.BleedingDays);
        Assert.Equal(2100, summary.AverageCaloriesOnBleedingDays);
        Assert.Equal(1800, summary.AverageCaloriesOnNonBleedingCycleDays);
        Assert.Equal(18, summary.AverageFiberOnBleedingDays);
        Assert.Equal(28, summary.AverageFiberOnNonBleedingCycleDays);
        Assert.Equal(8, summary.AveragePainImpactOnDaysWithMeals);
        Assert.False(summary.HasEnoughNutritionData);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithEnoughGroupData_MarksSummaryReliable() {
        var user = User.Create("cycle-nutrition-enough@example.com", "hash");
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.UpsertBleedingEntry(startDate, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 8, notes: null);
        profile.UpsertBleedingEntry(startDate.AddDays(1), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 6, notes: null);
        profile.UpsertSymptomEntry(startDate.AddDays(2), CycleSymptomCategory.Craving, 4, [], note: null);
        profile.UpsertSymptomEntry(startDate.AddDays(3), CycleSymptomCategory.Energy, 5, [], note: null);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([
                CreateNutritionBucket(startDate, calories: 2100, fiber: 18),
                CreateNutritionBucket(startDate.AddDays(1), calories: 2000, fiber: 20),
                CreateNutritionBucket(startDate.AddDays(2), calories: 1800, fiber: 28),
                CreateNutritionBucket(startDate.AddDays(3), calories: 1900, fiber: 26),
            ]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(4)),
            CancellationToken.None);

        ResultAssert.Success(result);
        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(result.Value);
        Assert.True(summary.HasEnoughNutritionData);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithMissingCycle_ReturnsNull() {
        var user = User.Create("cycle-nutrition-missing@example.com", "hash");
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WhenStatisticsFails_ReturnsFailure() {
        var user = User.Create("cycle-statistics-failure@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc));
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateFailingStatisticsReadService(Errors.Validation.Invalid("statistics", "Statistics unavailable.")),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithEmptyUserId_ReturnsInvalidToken() {
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(User.Create("cycle-nutrition-empty-user@example.com", "hash")));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(Guid.Empty, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.InvalidToken", result.Error.Code);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithInvertedDates_ReturnsValidationFailure() {
        var user = User.Create("cycle-nutrition-inverted@example.com", "hash");
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateTime.UtcNow, DateTime.UtcNow.AddDays(-1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("DateFrom", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithTooLargeRange_ReturnsValidationFailure() {
        var user = User.Create("cycle-nutrition-long-range@example.com", "hash");
        DateTime from = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, from, from.AddDays(367)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("one year", result.Error.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithDeletedUser_ReturnsAccountDeleted() {
        var user = User.Create("cycle-nutrition-deleted@example.com", "hash");
        user.MarkDeleted(DateTime.UtcNow);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateTime.UtcNow.AddDays(-7), DateTime.UtcNow),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithFertilitySignalOnly_IncludesLoggedDay() {
        var user = User.Create("cycle-nutrition-fertility@example.com", "hash");
        DateTime startDate = new(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.UpsertFertilitySignal(startDate.AddDays(1), 36.62, OvulationTestResult.Positive, "egg white", hadSex: true, notes: null);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([CreateNutritionBucket(startDate.AddDays(1), calories: 1900, fiber: 22)]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(2)),
            CancellationToken.None);

        ResultAssert.Success(result);
        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(result.Value);
        Assert.Equal(1, summary.LoggedCycleDays);
        Assert.Equal(1, summary.DaysWithMeals);
        Assert.Equal(0, summary.BleedingDays);
        Assert.Equal(1900, summary.AverageCaloriesOnNonBleedingCycleDays);
    }
}
