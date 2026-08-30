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
        DateOnly startDate = new(2026, 4, 1);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, DateTime.UtcNow);
        profile.UpsertBleedingEntry(startDate, BleedingType.Bleeding, CycleFlowLevel.Heavy, painImpact: 8, notes: null);
        profile.UpsertSymptomEntry(startDate.AddDays(1), CycleSymptomCategory.Craving, 6, ["sweet"], note: null);
        profile.UpsertBleedingEntry(startDate.AddDays(28), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([
                CreateNutritionBucket(startDate, calories: 2100, fiber: 18),
                CreateNutritionBucket(startDate.AddDays(1), calories: 1800, fiber: 28),
            ]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(29)),
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
        DateOnly startDate = new(2026, 4, 1);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, DateTime.UtcNow);
        foreach (int offset in (int[])[0, 28, 56, 84]) {
            profile.UpsertBleedingEntry(startDate.AddDays(offset), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 6, notes: null);
        }

        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([
                CreateNutritionBucket(startDate, calories: 2100, fiber: 18),
                CreateNutritionBucket(startDate.AddDays(1), calories: 1800, fiber: 28),
                CreateNutritionBucket(startDate.AddDays(28), calories: 2050, fiber: 20),
                CreateNutritionBucket(startDate.AddDays(29), calories: 1850, fiber: 26),
                CreateNutritionBucket(startDate.AddDays(56), calories: 2000, fiber: 22),
                CreateNutritionBucket(startDate.AddDays(57), calories: 1900, fiber: 24),
            ]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(84)),
            CancellationToken.None);

        ResultAssert.Success(result);
        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(result.Value);
        Assert.True(summary.HasEnoughNutritionData);
        Assert.Equal(3, summary.CompletedCyclesAnalyzed);
        Assert.Equal(3, summary.ComparableCycles);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithMissingCycle_ReturnsNull() {
        var user = User.Create("cycle-nutrition-missing@example.com", "hash");
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7), DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        ResultAssert.Success(result);
        Assert.Null(result.Value);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithoutNutritionConsent_ReturnsConsentRequiredSummary() {
        var user = User.Create("cycle-nutrition-consent-required@example.com", "hash");
        DateOnly from = new(2026, 4, 1);
        var profile = CycleProfile.Create(user.Id, from);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, from, from.AddDays(7)),
            CancellationToken.None);

        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(ResultAssert.Success(result));
        Assert.Multiple(
            () => Assert.True(summary.ConsentRequired),
            () => Assert.Equal("Unavailable", summary.DataSufficiency),
            () => Assert.Contains("nutrition_consent_required", summary.ReasonCodes!, StringComparer.Ordinal));
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithoutCompletedIntervals_ReturnsInsufficientSummary() {
        var user = User.Create("cycle-nutrition-no-intervals@example.com", "hash");
        DateOnly from = new(2026, 4, 1);
        var profile = CycleProfile.Create(user.Id, from);
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, DateTime.UtcNow);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, from, from.AddDays(7)),
            CancellationToken.None);

        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(ResultAssert.Success(result));
        Assert.Multiple(
            () => Assert.False(summary.ConsentRequired),
            () => Assert.Equal("Insufficient", summary.DataSufficiency),
            () => Assert.Equal(0, summary.CompletedCyclesAnalyzed));
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WhenStatisticsFails_ReturnsFailure() {
        var user = User.Create("cycle-statistics-failure@example.com", "hash");
        var profile = CycleProfile.Create(user.Id, new DateOnly(2026, 4, 1));
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, DateTime.UtcNow);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateFailingStatisticsReadService(Errors.Validation.Invalid("statistics", "Statistics unavailable.")),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7), DateOnly.FromDateTime(DateTime.UtcNow)),
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
            new GetCycleNutritionSummaryQuery(Guid.Empty, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7), DateOnly.FromDateTime(DateTime.UtcNow)),
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
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-1)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Validation.Invalid", result.Error.Code);
        Assert.Contains("DateFrom", result.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithTooLargeRange_ReturnsValidationFailure() {
        var user = User.Create("cycle-nutrition-long-range@example.com", "hash");
        DateOnly from = new(2025, 1, 1);
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
            new GetCycleNutritionSummaryQuery(user.Id.Value, DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-7), DateOnly.FromDateTime(DateTime.UtcNow)),
            CancellationToken.None);

        ResultAssert.Failure(result);
        Assert.Equal("Authentication.AccountDeleted", result.Error.Code);
    }

    [Fact]
    public async Task GetCycleNutritionSummaryQueryHandler_WithFertilitySignalOnly_IncludesLoggedDay() {
        var user = User.Create("cycle-nutrition-fertility@example.com", "hash");
        DateOnly startDate = new(2026, 4, 1);
        var profile = CycleProfile.Create(user.Id, startDate);
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, DateTime.UtcNow);
        profile.GrantConsent(CycleConsentPurpose.FertilitySignals, DateTime.UtcNow);
        profile.UpsertBleedingEntry(startDate, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        profile.UpsertFertilitySignal(startDate.AddDays(1), 36.62, OvulationTestResult.Positive, "egg white", hadSex: true, notes: null);
        profile.UpsertBleedingEntry(startDate.AddDays(28), BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: null, notes: null);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile),
            CreateStatisticsReadService([CreateNutritionBucket(startDate.AddDays(1), calories: 1900, fiber: 22)]),
            CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, startDate, startDate.AddDays(29)),
            CancellationToken.None);

        ResultAssert.Success(result);
        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(result.Value);
        Assert.Equal(1, summary.LoggedCycleDays);
        Assert.Equal(1, summary.DaysWithMeals);
        Assert.Equal(1, summary.BleedingDays);
        Assert.Equal(0, summary.AverageCaloriesOnNonBleedingCycleDays);
        Assert.Contains("at_least_three_comparable_cycles_required", summary.ReasonCodes!, StringComparer.Ordinal);
    }
}
