using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Ai.Mappings;

public static class AiHttpResponseMappings {
    extension(FoodVisionModel model) {
        public FoodVisionHttpResponse ToHttpResponse() {
            return new FoodVisionHttpResponse(
                model.Items.ToHttpResponseList(ToHttpModel),
                model.Notes
            );
        }
    }

    extension(FoodNutritionModel model) {
        public FoodNutritionHttpResponse ToHttpResponse() {
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
    }

    extension(UserAiUsageModel model) {
        public UserAiUsageHttpResponse ToHttpResponse() {
            return new UserAiUsageHttpResponse(
                model.InputLimit,
                model.OutputLimit,
                model.InputUsed,
                model.OutputUsed,
                model.ResetAtUtc
            );
        }
    }

    extension(FoodVisionItemModel model) {
        private FoodVisionItemHttpModel ToHttpModel() {
            return new FoodVisionItemHttpModel(
                model.NameEn,
                model.NameLocal,
                model.Amount,
                model.Unit,
                model.Confidence,
                model.CenterX,
                model.CenterY,
                model.LocationConfidence
            );
        }
    }

    extension(FoodNutritionItemModel model) {
        private FoodNutritionItemHttpResponse ToHttpResponse() {
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
}
