using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Ai.Mappings;

public static class AiHttpResponseMappings {
    public static FoodVisionHttpResponse ToHttpResponse(this FoodVisionModel model) {
        return new FoodVisionHttpResponse(
            model.Items.ToHttpResponseList(ToHttpModel),
            model.Notes
        );
    }

    public static FoodNutritionHttpResponse ToHttpResponse(this FoodNutritionModel model) {
        return new FoodNutritionHttpResponse(
            model.Calories,
            model.Protein,
            model.Fat,
            model.Carbs,
            model.Fiber,
            model.Alcohol,
            model.Items.ToHttpResponseList(ToHttpResponse),
            model.Notes
        );
    }

    public static UserAiUsageHttpResponse ToHttpResponse(this UserAiUsageModel model) {
        return new UserAiUsageHttpResponse(
            model.InputLimit,
            model.OutputLimit,
            model.InputUsed,
            model.OutputUsed,
            model.ResetAtUtc
        );
    }

    private static FoodVisionItemHttpModel ToHttpModel(this FoodVisionItemModel model) {
        return new FoodVisionItemHttpModel(
            model.NameEn,
            model.NameLocal,
            model.Amount,
            model.Unit,
            model.Confidence
        );
    }

    private static FoodNutritionItemHttpResponse ToHttpResponse(this FoodNutritionItemModel model) {
        return new FoodNutritionItemHttpResponse(
            model.Name,
            model.Amount,
            model.Unit,
            model.Calories,
            model.Protein,
            model.Fat,
            model.Carbs,
            model.Fiber,
            model.Alcohol
        );
    }
}
