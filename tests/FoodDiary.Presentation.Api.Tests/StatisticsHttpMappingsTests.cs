using FoodDiary.Application.Statistics.Models;
using FoodDiary.Application.Statistics.Queries.GetStatistics;
using FoodDiary.Presentation.Api.Features.Statistics.Mappings;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;
using FoodDiary.Presentation.Api.Features.Statistics.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class StatisticsHttpMappingsTests {
    [Fact]
    public void GetStatisticsHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var httpQuery = new GetStatisticsHttpQuery(from, to, 7);

        GetStatisticsQuery query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
        Assert.Equal(7, query.QuantizationDays);
    }

    [Fact]
    public void AggregatedStatisticsModel_ToHttpResponse_MapsAllFields() {
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc);
        var model = new AggregatedStatisticsModel(from, to, 14000, 120, 80, 250, 25, 840, 560, 1750, 175);

        AggregatedStatisticsHttpResponse response = model.ToHttpResponse();

        Assert.Equal(from, response.DateFrom);
        Assert.Equal(to, response.DateTo);
        Assert.Equal(14000, response.TotalCalories);
        Assert.Equal(120, response.AverageProteins);
        Assert.Equal(80, response.AverageFats);
        Assert.Equal(250, response.AverageCarbs);
        Assert.Equal(25, response.AverageFiber);
        Assert.Equal(840, response.TotalProteins);
        Assert.Equal(560, response.TotalFats);
        Assert.Equal(1750, response.TotalCarbs);
        Assert.Equal(175, response.TotalFiber);
    }
}
