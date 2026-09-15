using FoodDiary.Modules.Tdee.Contracts.Models;
using FoodDiary.Modules.Tdee.Presentation.Contracts.Responses;

namespace FoodDiary.Modules.Tdee.Presentation.Mappings.Mappings;

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
