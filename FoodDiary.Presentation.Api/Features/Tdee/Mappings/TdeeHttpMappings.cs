using FoodDiary.Application.Tdee.Models;
using FoodDiary.Application.Tdee.Queries.GetTdeeInsight;
using FoodDiary.Presentation.Api.Features.Tdee.Responses;

namespace FoodDiary.Presentation.Api.Features.Tdee.Mappings;

public static class TdeeHttpMappings {
    public static GetTdeeInsightQuery ToTdeeQuery(this Guid userId) => new(userId);

    public static TdeeInsightHttpResponse ToHttpResponse(this TdeeInsightModel model) =>
        new(
            model.EstimatedTdee,
            model.AdaptiveTdee,
            model.Bmr,
            model.SuggestedCalorieTarget,
            model.CurrentCalorieTarget,
            model.WeightTrendPerWeek,
            model.Confidence.ToString().ToLowerInvariant(),
            model.DataDaysUsed,
            model.GoalAdjustmentHint);
}
