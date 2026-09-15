using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Modules.Statistics.Application.Models;
using FoodDiary.Modules.Statistics.Application.Queries.GetStatisticsSummary;
using FoodDiary.Modules.BodyMetrics.Contracts.WaistEntries.Models;
using FoodDiary.Modules.BodyMetrics.Contracts.WeightEntries.Models;
using FoodDiary.Mediator;
using FoodDiary.Modules.Statistics.Presentation.Controllers;
using FoodDiary.Modules.Statistics.Presentation.Requests;
using FoodDiary.Modules.Statistics.Presentation.Responses;
using FoodDiary.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Statistics.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class StatisticsControllerTests {
    [Fact]
    public async Task GetSummary_SendsQueryAndReturnsMappedResponse() {
        var from = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc);
        var to = new DateTime(2026, 8, 7, 0, 0, 0, DateTimeKind.Utc);
        var model = new StatisticsSummaryModel(
            [new AggregatedStatisticsModel(from, to, 14000, 120, 80, 250, 25)],
            [new WeightEntrySummaryModel(from, to, 75.3)],
            [new WaistEntrySummaryModel(from, to, 82.1)]);
        IRequest<Result<StatisticsSummaryModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        var controller = new StatisticsController(sender) {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
        };
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetSummary(userId, new GetStatisticsHttpQuery(from, to, 7));

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        StatisticsSummaryHttpResponse response = Assert.IsType<StatisticsSummaryHttpResponse>(ok.Value);
        GetStatisticsSummaryQuery query = Assert.IsType<GetStatisticsSummaryQuery>(sentRequest);
        Assert.Multiple(
            () => Assert.Equal(userId, query.UserId),
            () => Assert.Equal(from, query.DateFrom),
            () => Assert.Equal(to, query.DateTo),
            () => Assert.Equal(7, query.QuantizationDays),
            () => Assert.Equal(14000, Assert.Single(response.Nutrition).TotalCalories),
            () => Assert.Equal(75.3, Assert.Single(response.Weight).AverageWeightKg),
            () => Assert.Equal(82.1, Assert.Single(response.Waist).AverageCircumferenceCm));
    }
}
