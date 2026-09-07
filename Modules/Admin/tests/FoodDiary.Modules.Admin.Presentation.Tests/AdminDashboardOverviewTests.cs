using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Admin.Queries.GetAdminDashboardOverview;
using FoodDiary.Presentation.Api.Features.Admin;
using FoodDiary.Presentation.Api.Features.Admin.Requests;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class AdminDashboardOverviewTests {
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetOverview_MapsCurrentPreviousTrendsAndCurrencyBreakdown(bool previous) {
        DateTime from = new(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var currency = new AdminBillingRevenueCurrencyReadModel("USD", 100, 5, 2, 3, 90, 4, 6, 7, 8, 2);
        var period = new AdminDashboardPeriodModel(from, from.AddDays(1),
            new AdminDashboardMetrics(2, 3, 400, [new AdminDashboardTrend(from, 2, 400, [new AdminDashboardRevenuePoint("USD", 100)])]), [currency]);
        var model = new AdminDashboardOverviewModel(from, from.AddDays(1), "day", period, previous ? period : null, 50, 10, 2);
        CapturedSender sender = SubstituteSender.Capture(Result.Success(model));
        var controller = new AdminDashboardController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };

        IActionResult result = await controller.GetOverview(new GetAdminDashboardOverviewHttpQuery());

        AdminDashboardOverviewHttpResponse response = Assert.IsType<AdminDashboardOverviewHttpResponse>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.IsType<GetAdminDashboardOverviewQuery>(sender.Request);
        Assert.Multiple(
            () => Assert.Equal(model.FromUtc, response.FromUtc),
            () => Assert.Equal(model.ToUtc, response.ToUtc),
            () => Assert.Equal("day", response.Interval),
            () => Assert.Equal(50, response.TotalUsersNow),
            () => Assert.Equal(10, response.PremiumUsersNow),
            () => Assert.Equal(2, response.PendingReportsNow),
            () => Assert.Equal(previous, response.Previous is not null),
            () => Assert.Equal(2, response.Period.Metrics.Registrations),
            () => Assert.Equal(3, response.Period.Metrics.PayingUsers),
            () => Assert.Equal(400, response.Period.Metrics.AiTokens));
        AdminDashboardTrendHttpResponse trend = Assert.Single(response.Period.Metrics.Trend);
        AdminBillingRevenueCurrencyHttpResponse revenue = Assert.Single(response.Period.Currencies);
        Assert.Multiple(
            () => Assert.Equal(from, trend.Date),
            () => Assert.Equal(2, trend.Registrations),
            () => Assert.Equal(400, trend.AiTokens),
            () => Assert.Equal(new AdminDashboardRevenuePointHttpResponse("USD", 100), Assert.Single(trend.Revenue)),
            () => Assert.Equal(new AdminBillingRevenueCurrencyHttpResponse("USD", 100, 5, 2, 3, 90, 4, 6, 7, 8, 2), revenue));
    }
}
