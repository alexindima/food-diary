using FoodDiary.Application.WeeklyCheckIn.Models;
using FoodDiary.Presentation.Api.Features.WeeklyCheckIn.Mappings;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class WeeklyCheckInHttpMappingsTests {
    [Fact]
    public void ToQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        var query = userId.ToQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void WeeklyCheckInModel_ToHttpResponse_MapsAllFields() {
        var thisWeek = new WeekSummaryModel(14000, 2000, 120, 80, 250, 21, 7, 75.0, 74.5, 80.0, 79.5, 14000, 2000);
        var lastWeek = new WeekSummaryModel(13000, 1857, 110, 85, 230, 18, 6, 75.5, 75.0, 80.5, 80.0, 12000, 1714);
        var trends = new WeekTrendModel(1000, 10, -5, 20, -0.5, -0.5, 2000, 3);
        var suggestions = new List<string> { "Keep up the good work!", "Try to log more meals" };
        var model = new WeeklyCheckInModel(thisWeek, lastWeek, trends, suggestions);

        var response = model.ToHttpResponse();

        Assert.Equal(14000, response.ThisWeek.TotalCalories);
        Assert.Equal(2000, response.ThisWeek.AvgDailyCalories);
        Assert.Equal(120, response.ThisWeek.AvgProteins);
        Assert.Equal(7, response.ThisWeek.DaysLogged);
        Assert.Equal(75.0, response.ThisWeek.WeightStart);
        Assert.Equal(14000, response.ThisWeek.TotalHydrationMl);

        Assert.Equal(13000, response.LastWeek.TotalCalories);
        Assert.Equal(6, response.LastWeek.DaysLogged);

        Assert.Equal(1000, response.Trends.CalorieChange);
        Assert.Equal(-0.5, response.Trends.WeightChange);
        Assert.Equal(2000, response.Trends.HydrationChange);

        Assert.Equal(2, response.Suggestions.Count);
    }

    [Fact]
    public void WeeklyCheckInModel_ToHttpResponse_WithNullWeightAndWaist() {
        var summary = new WeekSummaryModel(0, 0, 0, 0, 0, 0, 0, null, null, null, null, 0, 0);
        var trends = new WeekTrendModel(0, 0, 0, 0, null, null, 0, 0);
        var model = new WeeklyCheckInModel(summary, summary, trends, []);

        var response = model.ToHttpResponse();

        Assert.Null(response.ThisWeek.WeightStart);
        Assert.Null(response.ThisWeek.WaistEnd);
        Assert.Null(response.Trends.WeightChange);
        Assert.Empty(response.Suggestions);
    }
}
