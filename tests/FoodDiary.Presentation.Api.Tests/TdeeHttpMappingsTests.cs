using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Presentation.Api.Features.Tdee.Mappings;
using FoodDiary.Presentation.Api.Features.Tdee.Responses;

namespace FoodDiary.Presentation.Api.Tests;

[ExcludeFromCodeCoverage]
public sealed class TdeeHttpMappingsTests {
    [Fact]
    public void ToTdeeQuery_MapsUserId() {
        var userId = Guid.NewGuid();

        GetTdeeInsightQuery query = userId.ToTdeeQuery();

        Assert.Equal(userId, query.UserId);
    }

    [Fact]
    public void TdeeInsightModel_ToHttpResponse_MapsAllFields() {
        var model = new TdeeInsightModel(2200, 2150, 1600, 1800, 2000, -0.3, TdeeConfidence.High, 28, "Reduce by 200 kcal");

        TdeeInsightHttpResponse response = model.ToHttpResponse();

        Assert.Equal(2200, response.EstimatedTdee);
        Assert.Equal(2150, response.AdaptiveTdee);
        Assert.Equal(1600, response.Bmr);
        Assert.Equal(1800, response.SuggestedCalorieTarget);
        Assert.Equal(2000, response.CurrentCalorieTarget);
        Assert.Equal(-0.3, response.WeightTrendPerWeek);
        Assert.Equal("high", response.Confidence);
        Assert.Equal(28, response.DataDaysUsed);
        Assert.Equal("Reduce by 200 kcal", response.GoalAdjustmentHint);
    }

    [Fact]
    public void TdeeInsightModel_ToHttpResponse_WithNullValues() {
        var model = new TdeeInsightModel(EstimatedTdee: null, AdaptiveTdee: null, Bmr: null, SuggestedCalorieTarget: null, CurrentCalorieTarget: null, WeightTrendPerWeek: null, TdeeConfidence.None, 0, GoalAdjustmentHint: null);

        TdeeInsightHttpResponse response = model.ToHttpResponse();

        Assert.Null(response.EstimatedTdee);
        Assert.Null(response.Bmr);
        Assert.Equal("none", response.Confidence);
        Assert.Null(response.GoalAdjustmentHint);
    }
}
