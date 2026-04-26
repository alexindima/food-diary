using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Application.Ai.Commands.ParseFoodText;
using FoodDiary.Application.Abstractions.Ai.Models;
using FoodDiary.Application.Ai.Queries.GetUserAiUsageSummary;
using FoodDiary.Presentation.Api.Features.Ai.Models;
using FoodDiary.Presentation.Api.Features.Ai.Requests;

namespace FoodDiary.Presentation.Api.Features.Ai.Mappings;

public static class AiHttpMappings {
    public static GetUserAiUsageSummaryQuery ToUsageQuery(this Guid userId) => new(userId);

    public static AnalyzeFoodImageCommand ToCommand(this FoodVisionHttpRequest request, Guid userId) {
        return new AnalyzeFoodImageCommand(
            UserId: userId,
            ImageAssetId: request.ImageAssetId,
            Description: request.Description);
    }

    public static ParseFoodTextCommand ToCommand(this FoodTextHttpRequest request, Guid userId) {
        return new ParseFoodTextCommand(UserId: userId, Text: request.Text);
    }

    public static CalculateFoodNutritionCommand ToCommand(this FoodNutritionHttpRequest request, Guid userId) {
        return new CalculateFoodNutritionCommand(
            UserId: userId,
            Items: request.Items?.Select(ToModel).ToList() ?? []);
    }

    private static FoodVisionItemModel ToModel(this FoodVisionItemHttpModel model) {
        return new FoodVisionItemModel(
            model.NameEn,
            model.NameLocal,
            model.Amount,
            model.Unit,
            model.Confidence);
    }
}
