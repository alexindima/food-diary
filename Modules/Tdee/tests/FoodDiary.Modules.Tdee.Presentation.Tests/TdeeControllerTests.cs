using FoodDiary.Presentation.Api.Tests;
using FoodDiary.Results;
using FoodDiary.Modules.Tdee.Contracts.Models;
using FoodDiary.Modules.Tdee.Contracts.Queries.GetTdeeInsight;
using FoodDiary.Mediator;
using FoodDiary.Modules.Tdee.Presentation.Controllers;
using FoodDiary.Modules.Tdee.Presentation.Contracts.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FoodDiary.Modules.Tdee.Presentation.Tests;

[ExcludeFromCodeCoverage]
public sealed class TdeeControllerTests {
    [Fact]
    public async Task GetInsight_SendsQueryAndReturnsInsight() {
        var model = new TdeeInsightModel(2200, 2150, 1600, 1800, 2000, -0.3, TdeeConfidence.High, 28, "Reduce by 200 kcal");
        IRequest<Result<TdeeInsightModel>>? sentRequest = null;
        ISender sender = SubstituteSender.Create(Result.Success(model), request => sentRequest = request);
        TdeeController controller = CreateController(sender);
        var userId = Guid.NewGuid();

        IActionResult result = await controller.GetInsight(userId);

        OkObjectResult ok = Assert.IsType<OkObjectResult>(result);
        TdeeInsightHttpResponse response = Assert.IsType<TdeeInsightHttpResponse>(ok.Value);
        Assert.Equal(2200, response.EstimatedTdee);
        Assert.Equal("high", response.Confidence);
        GetTdeeInsightQuery query = Assert.IsType<GetTdeeInsightQuery>(sentRequest);
        Assert.Equal(userId, query.UserId);
    }

    private static TdeeController CreateController(ISender sender) =>
        new(sender) {
            ControllerContext = new ControllerContext {
                HttpContext = new DefaultHttpContext(),
            },
        };
}
