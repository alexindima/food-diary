using FoodDiary.Application.Statistics.Models;
using FoodDiary.Presentation.Api.Features.Statistics.Mappings;
using FoodDiary.Presentation.Api.Features.Statistics.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class StatisticsHttpMappingsTests {
    [Fact]
    public void GetStatisticsHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 31, 0, 0, 0, DateTimeKind.Utc);
        var httpQuery = new GetStatisticsHttpQuery(from, to, 7);

        var query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(from, query.DateFrom);
        Assert.Equal(to, query.DateTo);
        Assert.Equal(7, query.QuantizationDays);
    }

    [Fact]
    public void AggregatedStatisticsModel_ToHttpResponse_MapsAllFields() {
        var from = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 3, 7, 0, 0, 0, DateTimeKind.Utc);
        var model = new AggregatedStatisticsModel(from, to, 14000, 120, 80, 250, 25);

        var response = model.ToHttpResponse();

        Assert.Equal(from, response.DateFrom);
        Assert.Equal(to, response.DateTo);
        Assert.Equal(14000, response.TotalCalories);
        Assert.Equal(120, response.AverageProteins);
        Assert.Equal(80, response.AverageFats);
        Assert.Equal(250, response.AverageCarbs);
        Assert.Equal(25, response.AverageFiber);
    }
}
