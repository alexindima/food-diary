using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Application.Ai.Commands.ParseFoodText;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Presentation.Api.Features.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Requests;

namespace FoodDiary.Presentation.Api.Features.Ai.Mappings;

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
