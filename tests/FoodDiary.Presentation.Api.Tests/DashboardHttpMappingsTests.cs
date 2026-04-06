using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Presentation.Api.Features.Dashboard.Mappings;
using FoodDiary.Presentation.Api.Features.Dashboard.Requests;

namespace FoodDiary.Presentation.Api.Tests;

public sealed class DashboardHttpMappingsTests {
    [Fact]
    public void GetDashboardSnapshotHttpQuery_ToQuery_MapsAllFields() {
        var userId = Guid.NewGuid();
        var date = new DateTime(2026, 4, 6, 0, 0, 0, DateTimeKind.Utc);
        var httpQuery = new GetDashboardSnapshotHttpQuery(date, 2, 20, "ru", 14);

        var query = httpQuery.ToQuery(userId);

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

        var query = httpQuery.ToQuery(userId);

        Assert.Equal(userId, query.UserId);
        Assert.Equal(date, query.Date);
        Assert.Equal("en", query.Locale);
    }

    [Fact]
    public void DailyAdviceModel_ToHttpResponse_MapsAllFields() {
        var id = Guid.NewGuid();
        var model = new DailyAdviceModel(id, "en", "Drink more water!", "hydration", 3);

        var response = model.ToHttpResponse();

        Assert.Equal(id, response.Id);
        Assert.Equal("en", response.Locale);
        Assert.Equal("Drink more water!", response.Value);
        Assert.Equal("hydration", response.Tag);
        Assert.Equal(3, response.Weight);
    }
}
