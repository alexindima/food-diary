using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.DailyAdvices.Models;
using FoodDiary.Application.DailyAdvices.Queries.GetDailyAdvice;
using FoodDiary.Application.Dashboard.Commands.SendDashboardTestEmail;
using FoodDiary.Application.Dashboard.Models;
using FoodDiary.Application.Dashboard.Queries.GetDashboardSnapshot;
using FoodDiary.Presentation.Api.Features.Dashboard;
using FoodDiary.Presentation.Api.Features.Dashboard.Requests;
using FoodDiary.Presentation.Api.Features.Dashboard.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class DashboardControllerTests {
    [Fact]
    public async Task Get_SendsDashboardQueryAndReturnsResponse() {
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        DashboardSnapshotModel model = CreateDashboardSnapshot(date);
        RecordingSender sender = new(Result.Success(model));
        DashboardController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        var query = new GetDashboardSnapshotHttpQuery(date, Page: 2, PageSize: 20, Locale: "ru", TrendDays: 14);

        IActionResult result = await controller.Get(userId, query);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        DashboardSnapshotHttpResponse response = Assert.IsType<DashboardSnapshotHttpResponse>(ok.Value);
        Assert.Equal(date, response.Date);
        Assert.Equal(2100, response.DailyGoal);
        GetDashboardSnapshotQuery sentQuery = Assert.IsType<GetDashboardSnapshotQuery>(sender.Request);
        Assert.Equal(userId, sentQuery.UserId);
        Assert.Equal(date, sentQuery.Date);
        Assert.Equal(2, sentQuery.Page);
        Assert.Equal(20, sentQuery.PageSize);
        Assert.Equal("ru", sentQuery.Locale);
        Assert.Equal(14, sentQuery.TrendDays);
    }

    [Fact]
    public async Task SendTestEmail_SendsCommandAndReturnsNoContent() {
        RecordingSender sender = new(Result.Success());
        DashboardController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.SendTestEmail(userId);

        Assert.IsType<NoContentResult>(result);
        SendDashboardTestEmailCommand command = Assert.IsType<SendDashboardTestEmailCommand>(sender.Request);
        Assert.Equal(userId, command.UserId);
    }

    [Fact]
    public async Task GetAdvice_SendsAdviceQueryAndReturnsResponse() {
        var adviceId = Guid.NewGuid();
        DailyAdviceModel model = new(adviceId, "en", "Drink water", "hydration", 3);
        RecordingSender sender = new(Result.Success(model));
        DashboardController controller = CreateController(sender);
        var userId = Guid.NewGuid();
        DateTime date = new(2026, 6, 14, 0, 0, 0, DateTimeKind.Utc);
        var query = new GetDailyAdviceHttpQuery(date, "en");

        IActionResult result = await controller.GetAdvice(userId, query);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        DailyAdviceHttpResponse response = Assert.IsType<DailyAdviceHttpResponse>(ok.Value);
        Assert.Equal(adviceId, response.Id);
        Assert.Equal("Drink water", response.Value);
        GetDailyAdviceQuery sentQuery = Assert.IsType<GetDailyAdviceQuery>(sender.Request);
        Assert.Equal(userId, sentQuery.UserId);
        Assert.Equal(date, sentQuery.Date);
        Assert.Equal("en", sentQuery.Locale);
    }

    private static DashboardController CreateController(RecordingSender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };

    private static DashboardSnapshotModel CreateDashboardSnapshot(DateTime date) =>
        new(
            date,
            date.AddDays(1).AddTicks(-1),
            DailyGoal: 2100,
            WeeklyCalorieGoal: 14700,
            new DashboardStatisticsModel(
                TotalCalories: 1800,
                AverageProteins: 120,
                AverageFats: 65,
                AverageCarbs: 180,
                AverageFiber: 25,
                ProteinGoal: 130,
                FatGoal: 70,
                CarbGoal: 200,
                FiberGoal: 30),
            [new DailyCaloriesModel(date, 1800)],
            new DashboardWeightModel(Latest: null, Previous: null, Desired: null),
            new DashboardWaistModel(Latest: null, Previous: null, Desired: null),
            new DashboardMealsModel([], Total: 0),
            Advice: new DailyAdviceModel(Guid.NewGuid(), "ru", "Advice", "tag", 1));
}
