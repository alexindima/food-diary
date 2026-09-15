using FoodDiary.Modules.Statistics.Presentation.Mappings;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Statistics.Application.Queries.GetStatistics;
using FoodDiary.Modules.Statistics.Application.Queries.GetStatisticsSummary;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;

using FoodDiary.Modules.Statistics.Presentation.Requests;
using FoodDiary.Modules.Statistics.Presentation.Responses;

namespace FoodDiary.Modules.Statistics.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class StatisticsHttpMappingsTests {
    [Fact]
    public void GetStatisticsHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var httpQuery = new GetStatisticsHttpQuery(from, to, 7);

        GetStatisticsQuery query = httpQuery.ToQuery(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(from, query.DateFrom),
            () => Assert.Equal(to, query.DateTo),
            () => Assert.Equal(7, query.QuantizationDays));
    }

    [Fact]
    public void AggregatedStatisticsModel_ToHttpResponse_MapsAllFields() {
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc);
        var model = new AggregatedStatisticsModel(from, to, 14000, 120, 80, 250, 25, 840, 560, 1750, 175);

        AggregatedStatisticsHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(from, response.DateFrom),
            () => Assert.Equal(to, response.DateTo),
            () => Assert.Equal(14000, response.TotalCalories),
            () => Assert.Equal(120, response.AverageProteins),
            () => Assert.Equal(80, response.AverageFats),
            () => Assert.Equal(250, response.AverageCarbs),
            () => Assert.Equal(25, response.AverageFiber),
            () => Assert.Equal(840, response.TotalProteins),
            () => Assert.Equal(560, response.TotalFats),
            () => Assert.Equal(1750, response.TotalCarbs),
            () => Assert.Equal(175, response.TotalFiber));
    }

    [Fact]
    public void GetStatisticsHttpQuery_ToSummaryQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var httpQuery = new GetStatisticsHttpQuery(from, to, 7);

        GetStatisticsSummaryQuery query = httpQuery.ToSummaryQuery(userId);

        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(from, query.DateFrom),
            () => Assert.Equal(to, query.DateTo),
            () => Assert.Equal(7, query.QuantizationDays));
    }

    [Fact]
    public void StatisticsSummaryModel_ToHttpResponse_MapsAllSections() {
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc);
        var model = new StatisticsSummaryModel(
            [new AggregatedStatisticsModel(from, to, 14000, 120, 80, 250, 25)],
            [new WeightEntrySummaryModel(from, to, 75.3)],
            [new WaistEntrySummaryModel(from, to, 82.1)]);

        StatisticsSummaryHttpResponse response = model.ToHttpResponse();

        Assert.Multiple(
            () => Assert.Equal(14000, Assert.Single(response.Nutrition).TotalCalories),
            () => Assert.Equal(75.3, Assert.Single(response.Weight).AverageWeightKg),
            () => Assert.Equal(82.1, Assert.Single(response.Waist).AverageCircumferenceCm));
    }
}
