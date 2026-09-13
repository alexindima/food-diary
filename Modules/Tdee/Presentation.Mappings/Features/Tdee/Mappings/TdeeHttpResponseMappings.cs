using FoodDiary.Application.Tdee.Models;
using FoodDiary.Presentation.Api.Features.Tdee.Responses;

namespace FoodDiary.Presentation.Api.Features.Tdee.Mappings;

public static class TdeeHttpResponseMappings {
    extension(TdeeInsightModel model) {
        public TdeeInsightHttpResponse ToHttpResponse() =>
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
}
