using FoodDiary.Modules.Ai.Application.Commands.AnalyzeFoodImage;
using FoodDiary.Modules.Ai.Application.Commands.CalculateFoodNutrition;
using FoodDiary.Modules.Ai.Application.Commands.ParseFoodText;
using FoodDiary.Modules.Ai.Contracts.Models;
using FoodDiary.Modules.Ai.Application.Queries.GetUserAiUsageSummary;
using FoodDiary.Modules.Ai.Presentation.Models;
using FoodDiary.Modules.Ai.Presentation.Requests;

namespace FoodDiary.Modules.Ai.Presentation.Mappings;

public static class AiHttpMappings {
    extension(Guid userId) {
        public GetUserAiUsageSummaryQuery ToUsageQuery() => new(userId);
    }

    extension(FoodVisionHttpRequest request) {
        public AnalyzeFoodImageCommand ToCommand(Guid userId, string requestId) {
            return new AnalyzeFoodImageCommand(
                UserId: userId,
                ImageAssetId: request.ImageAssetId,
                Description: request.Description,
                RequestId: requestId);
        }
    }

    extension(FoodTextHttpRequest request) {
        public ParseFoodTextCommand ToCommand(Guid userId, string requestId) {
            return new ParseFoodTextCommand(UserId: userId, Text: request.Text, RequestId: requestId);
        }
    }

    extension(FoodNutritionHttpRequest request) {
        public CalculateFoodNutritionCommand ToCommand(Guid userId, string requestId) {
            return new CalculateFoodNutritionCommand(
                UserId: userId,
                Items: request.Items.Select(ToModel).ToList(),
                RequestId: requestId);
        }
    }

    extension(FoodVisionItemHttpModel model) {
        private FoodVisionItemModel ToModel() {
            return new FoodVisionItemModel(
                model.NameEn,
                model.NameLocal,
                model.Amount,
                model.Unit,
                model.Confidence,
                model.CenterX,
                model.CenterY,
                model.LocationConfidence);
        }
    }
}
