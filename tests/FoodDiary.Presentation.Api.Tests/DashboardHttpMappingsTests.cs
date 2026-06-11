using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.Cycles.Models;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Hydration.Models;
using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Users.Models;
using FoodDiary.Application.WaistEntries.Models;
using FoodDiary.Application.WeightEntries.Models;
using FoodDiary.Presentation.Api.Features.Dashboard.Mappings;
using FoodDiary.Presentation.Api.Features.Dashboard.Requests;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Presentation.Api.Features.Dashboard.Responses;
using FoodDiary.Domain.Enums;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class DashboardHttpMappingsTests {
    [Fact]
    public void GetDashboardSnapshotHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var httpQuery = new GetDashboardSnapshotHttpQuery(date, 2, 20, "ru", 14);

        GetDashboardSnapshotQuery query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(date, query.Date);
        Assert.Equal(2, query.Page);
        Assert.Equal(20, query.PageSize);
        Assert.Equal("ru", query.Locale);
        Assert.Equal(14, query.TrendDays);
    }

    [Fact]
    public void GetDailyAdviceHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var httpQuery = new GetDailyAdviceHttpQuery(date, "en");

        GetDailyAdviceQuery query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(date, query.Date);
        Assert.Equal("en", query.Locale);
    }

    [Fact]
    public void DailyAdviceModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var model = new DailyAdviceModel(id, "en", "Drink more water!", "hydration", 3);

        DailyAdviceHttpResponse response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal("en", response.Locale);
        Assert.Equal("Drink more water!", response.Value);
        Assert.Equal("hydration", response.Tag);
        Assert.Equal(3, response.Weight);
    }

    [Fact]
    public void DashboardSnapshotModel_ToHttpResponse_MapsNestedSections() {
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        DateTime dateTo = date.AddDays(1).AddTicks(-1);
        var cycleId = Guid.NewGuid();
        var bleedingId = Guid.NewGuid();
        var model = new DashboardSnapshotModel(
            date,
            dateTo,
            DailyGoal: 2100,
            WeeklyCalorieGoal: 14700,
            new DashboardStatisticsModel(
                TotalCalories: 1850,
                AverageProteins: 120,
                AverageFats: 65,
                AverageCarbs: 180,
                AverageFiber: 25,
                ProteinGoal: 130,
                FatGoal: 70,
                CarbGoal: 200,
                FiberGoal: 30),
            [new DailyCaloriesModel(date, 1850)],
            new DashboardWeightModel(
                new WeightPointModel(date, 80.5),
                new WeightPointModel(date.AddDays(-1), 81.2),
                Desired: 76),
            new DashboardWaistModel(
                new WaistPointModel(date, 88.3),
                new WaistPointModel(date.AddDays(-1), 89.1),
                Desired: 84),
            new DashboardMealsModel([], Total: 0),
            new HydrationDailyModel(date, TotalMl: 1800, GoalMl: 2500),
            new DailyAdviceModel(Guid.NewGuid(), "ru", "Совет", "hydration", 2),
            CurrentFastingSession: null,
            [new WeightEntrySummaryModel(date.AddDays(-7), date, 80.9)],
            [new WaistEntrySummaryModel(date.AddDays(-7), date, 88.8)],
            new DashboardLayoutModel(["stats", "meals"], ["hydration"]),
            CaloriesBurned: 420,
            new TdeeInsightModel(
                EstimatedTdee: 2400,
                AdaptiveTdee: 2350,
                Bmr: 1700,
                SuggestedCalorieTarget: 2100,
                CurrentCalorieTarget: 2200,
                WeightTrendPerWeek: -0.2,
                TdeeConfidence.Medium,
                DataDaysUsed: 21,
                GoalAdjustmentHint: "keep"),
            new CycleModel(
                cycleId,
                Guid.NewGuid(),
                CycleTrackingMode.PeriodTracking,
                CycleConfidence.Medium,
                date.AddDays(-10),
                AverageCycleLength: 28,
                AveragePeriodLength: 5,
                LutealLength: 14,
                IsRegular: true,
                IsOnboardingComplete: true,
                ShowFertilityEstimates: false,
                DiscreetNotifications: true,
                Notes: "regular",
                [
                    new BleedingEntryModel(
                        bleedingId,
                        cycleId,
                        date,
                        BleedingType.Bleeding,
                        CycleFlowLevel.Medium,
                        PainImpact: 2,
                        Notes: "day notes"),
                ],
                Symptoms: [],
                Factors: [],
                FertilitySignals: [],
                new CyclePredictionsModel(date.AddDays(17), date.AddDays(19), OvulationFrom: null, OvulationTo: null, date.AddDays(13), date.AddDays(19), "Medium", "test")));

        DashboardSnapshotHttpResponse response = model.ToHttpResponse();

        Assert.Equal(model.Date, response.Date);
        Assert.Equal(model.DateTo, response.DateTo);
        Assert.Equal(2100, response.DailyGoal);
        Assert.Equal(1850, response.Statistics.TotalCalories);
        Assert.Single(response.WeeklyCalories);
        Assert.Equal(80.5, response.Weight.Latest!.Weight);
        Assert.Equal(88.3, response.Waist.Latest!.Circumference);
        Assert.Equal(0, response.Meals.Total);
        Assert.Equal(1800, response.Hydration!.TotalMl);
        Assert.Equal("Совет", response.Advice!.Value);
        Assert.Single(response.WeightTrend!);
        Assert.Single(response.WaistTrend!);
        Assert.Equal(["stats", "meals"], response.DashboardLayout!.Web);
        Assert.Equal(420, response.CaloriesBurned);
        Assert.Equal(2400, response.TdeeInsight!.EstimatedTdee);
        Assert.Equal(cycleId, response.CurrentCycle!.Id);
        Assert.Equal(bleedingId, response.CurrentCycle.BleedingEntries.First().Id);
        Assert.Equal(date.AddDays(17), response.CurrentCycle.Predictions!.NextPeriodStartFrom);
    }
}
