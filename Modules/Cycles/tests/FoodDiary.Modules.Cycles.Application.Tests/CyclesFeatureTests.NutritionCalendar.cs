using System.Globalization;
using FoodDiary.Modules.Cycles.Application.Queries.GetCycleNutritionSummary;
using FoodDiary.Modules.Cycles.Contracts.Models;
using FoodDiary.Modules.Cycles.Domain.Contracts.Enums;
using FoodDiary.Modules.Cycles.Domain.Entities;
using FoodDiary.Modules.Meals.Contracts.Common;
using FoodDiary.Modules.Meals.Contracts.Models;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Users.Domain.Entities;
using FoodDiary.Results;

namespace FoodDiary.Modules.Cycles.Application.Tests;

public partial class CyclesFeatureTests {
    [Theory]
    [InlineData("Asia/Tbilisi", "2026-08-29", "2026-08-28T20:00:00Z", "2026-08-29T20:00:00Z", "2026-08-30T20:00:00Z")]
    [InlineData("America/Los_Angeles", "2026-08-29", "2026-08-29T07:00:00Z", "2026-08-30T07:00:00Z", "2026-08-31T07:00:00Z")]
    [InlineData("Asia/Kathmandu", "2026-08-29", "2026-08-28T18:15:00Z", "2026-08-29T18:15:00Z", "2026-08-30T18:15:00Z")]
    [InlineData("Pacific/Kiritimati", "2026-08-29", "2026-08-28T10:00:00Z", "2026-08-29T10:00:00Z", "2026-08-30T10:00:00Z")]
    [InlineData("Europe/Berlin", "2026-03-29", "2026-03-28T23:00:00Z", "2026-03-29T22:00:00Z", "2026-03-30T22:00:00Z")]
    [InlineData("Europe/Berlin", "2026-10-25", "2026-10-24T22:00:00Z", "2026-10-25T23:00:00Z", "2026-10-26T23:00:00Z")]
    public async Task NutritionSummary_WithCalendarZone_UsesLocalDayBoundsAndBleedingDate(
        string zoneId, string dayKey, string fromInstant, string secondInstant, string untilInstant) {
        var user = User.Create("cycle-calendar@example.test", "hash");
        var day = DateOnly.Parse(dayKey, CultureInfo.InvariantCulture);
        DateTime from = DateTimeOffset.Parse(fromInstant, CultureInfo.InvariantCulture).UtcDateTime;
        DateTime second = DateTimeOffset.Parse(secondInstant, CultureInfo.InvariantCulture).UtcDateTime;
        DateTime until = DateTimeOffset.Parse(untilInstant, CultureInfo.InvariantCulture).UtcDateTime;
        var profile = CycleProfile.Create(user.Id, day);
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, DateTime.UtcNow);
        profile.UpsertBleedingEntry(day, BleedingType.Bleeding, CycleFlowLevel.Medium, painImpact: 0, notes: null);
        profile.ConfirmPeriodStart(day.AddDays(28));
        IMealNutritionStatisticsReadService statistics = Substitute.For<IMealNutritionStatisticsReadService>();
        IReadOnlyList<MealNutritionStatisticsBucket> buckets = [
            CreateNutritionBucket(from, calories: 200, fiber: 2),
            CreateNutritionBucket(second, calories: 400, fiber: 4),
        ];
        statistics.GetStatisticsAsync(Arg.Any<UserId>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(),
                Arg.Any<int>(), Arg.Any<CancellationToken>(), Arg.Any<TimeZoneInfo?>())
            .Returns(Task.FromResult(Result.Success(buckets)));
        using var cancellation = new CancellationTokenSource();
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile), statistics, CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, day, day.AddDays(1), zoneId), cancellation.Token);

        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(ResultAssert.Success(result));
        await statistics.Received(1).GetStatisticsAsync(user.Id, from, until.AddTicks(-1), 1,
            cancellation.Token, Arg.Is<TimeZoneInfo?>(zone => zone != null && zone.Id == zoneId));
        Assert.Multiple(
            () => Assert.Equal(day, summary.DateFrom),
            () => Assert.Equal(day.AddDays(1), summary.DateTo),
            () => Assert.Equal(2, summary.DaysWithMeals),
            () => Assert.Equal(1, summary.ComparableCycles),
            () => Assert.Equal(200, summary.AverageCaloriesOnBleedingDays),
            () => Assert.Equal(400, summary.AverageCaloriesOnNonBleedingCycleDays),
            () => Assert.Equal(2, summary.AverageFiberOnBleedingDays),
            () => Assert.Equal(4, summary.AverageFiberOnNonBleedingCycleDays),
            () => Assert.Equal(0, summary.AveragePainImpactOnDaysWithMeals));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Not/AZone")]
    public async Task NutritionSummary_WithInvalidZone_DoesNotReadNutrition(string zoneId) {
        var user = User.Create("cycle-invalid-zone@example.test", "hash");
        IMealNutritionStatisticsReadService statistics = CreateStatisticsReadService([]);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(), statistics, CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, new DateOnly(2026, 8, 29), new DateOnly(2026, 8, 30), zoneId),
            CancellationToken.None);

        Assert.Equal("Validation.Invalid", ResultAssert.Failure(result).Code);
        await statistics.DidNotReceiveWithAnyArgs().GetStatisticsAsync(default, default, default, default, default, default);
    }

    [Fact]
    public async Task NutritionSummary_WithoutOptionalZone_PreservesLegacyUtcBounds() {
        var user = User.Create("cycle-legacy-zone@example.test", "hash");
        DateOnly day = new(2026, 8, 29);
        var profile = CycleProfile.Create(user.Id, day);
        profile.GrantConsent(CycleConsentPurpose.NutritionInsights, DateTime.UtcNow);
        IMealNutritionStatisticsReadService statistics = CreateStatisticsReadService([]);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(profile), statistics, CreateCurrentUserAccessService(user));

        ResultAssert.Success(await handler.Handle(new GetCycleNutritionSummaryQuery(user.Id.Value, day, day), CancellationToken.None));

        await statistics.Received(1).GetStatisticsAsync(user.Id,
            day.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), day.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
            1, CancellationToken.None, timeZone: null);
    }

    [Fact]
    public async Task NutritionSummary_WithZoneWithoutConsent_DoesNotReadNutrition() {
        var user = User.Create("cycle-zone-consent@example.test", "hash");
        DateOnly day = new(2026, 8, 29);
        IMealNutritionStatisticsReadService statistics = CreateStatisticsReadService([]);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new InMemoryCycleRepository(CycleProfile.Create(user.Id, day)), statistics, CreateCurrentUserAccessService(user));

        CycleNutritionSummaryModel summary = Assert.IsType<CycleNutritionSummaryModel>(ResultAssert.Success(await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, day, day, "Asia/Tbilisi"), CancellationToken.None)));

        Assert.True(summary.ConsentRequired);
        await statistics.DidNotReceiveWithAnyArgs().GetStatisticsAsync(default, default, default, default, default, default);
    }

    [Theory]
    [InlineData("0001-01-01", "Asia/Tbilisi")]
    [InlineData("9999-12-31", "UTC")]
    public async Task NutritionSummary_WithUnrepresentableUtcBounds_ReturnsValidation(string dayKey, string zoneId) {
        var user = User.Create("cycle-calendar-extreme@example.test", "hash");
        var day = DateOnly.Parse(dayKey, CultureInfo.InvariantCulture);
        GetCycleNutritionSummaryQueryHandler handler = CreateCycleNutritionSummaryHandler(
            new NoopCycleRepository(), CreateStatisticsReadService([]), CreateCurrentUserAccessService(user));

        Result<CycleNutritionSummaryModel?> result = await handler.Handle(
            new GetCycleNutritionSummaryQuery(user.Id.Value, day, day, zoneId), CancellationToken.None);

        Assert.Equal("Validation.Invalid", ResultAssert.Failure(result).Code);
    }
}
