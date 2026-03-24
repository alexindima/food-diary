using FoodDiary.Application.Ai.Commands.AnalyzeFoodImage;
using FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Presentation.Api.Features.Ai.Requests;

namespace FoodDiary.Presentation.Api.Features.Ai.Mappings;

public static class AiHttpMappings {
    public static AnalyzeFoodImageCommand ToCommand(this FoodVisionHttpRequest request, UserId userId) {
        return new AnalyzeFoodImageCommand(
            userId,
            new ImageAssetId(request.ImageAssetId),
            request.Description);
    }

    public static CalculateFoodNutritionCommand ToCommand(this FoodNutritionHttpRequest request, UserId userId) {
        return new CalculateFoodNutritionCommand(userId, request.Items ?? Array.Empty<FoodDiary.Contracts.Ai.FoodVisionItem>());
    }
}
