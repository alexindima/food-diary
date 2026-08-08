using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Commands.RepeatMeal;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;

namespace FoodDiary.Presentation.Api.Features.Consumptions.Mappings;

public static class ConsumptionHttpMappings {
    extension(Guid consumptionId) {
        public DeleteConsumptionCommand ToDeleteCommand(Guid userId) =>
                new(userId, consumptionId);
    }

    extension(CreateConsumptionHttpRequest request) {
        public CreateConsumptionCommand ToCommand(Guid userId) =>
                new(
                    userId,
                    request.Date,
                    request.MealType,
                    request.Comment,
                    request.ImageUrl,
                    request.ImageAssetId,
                    ToItemInputs(request.Items),
                    ToAiSessionInputs(request.AiSessions),
                    request.IsNutritionAutoCalculated,
                    request.ManualCalories,
                    request.ManualProteins,
                    request.ManualFats,
                    request.ManualCarbs,
                    request.ManualFiber,
                    request.ManualAlcohol,
                    request.PreMealSatietyLevel,
                    request.PostMealSatietyLevel);
    }

    extension(UpdateConsumptionHttpRequest request) {
        public UpdateConsumptionCommand ToCommand(Guid userId, Guid consumptionId) =>
                new(
                    userId,
                    consumptionId,
                    request.Date,
                    request.MealType,
                    request.Comment,
                    request.ImageUrl,
                    request.ImageAssetId,
                    ToItemInputs(request.Items),
                    ToAiSessionInputs(request.AiSessions),
                    request.IsNutritionAutoCalculated,
                    request.ManualCalories,
                    request.ManualProteins,
                    request.ManualFats,
                    request.ManualCarbs,
                    request.ManualFiber,
                    request.ManualAlcohol,
                    request.PreMealSatietyLevel,
                    request.PostMealSatietyLevel);
    }

    private static ConsumptionItemInput ToInput(ConsumptionItemHttpRequest request) =>
        new(request.ProductId, request.RecipeId, request.Amount, request.SourceAiItemId, request.Origin);

    private static ConsumptionAiSessionInput ToInput(ConsumptionAiSessionHttpRequest request) =>
        new(
            request.ImageAssetId,
            request.Source,
            request.RecognizedAtUtc,
            request.Notes,
            ToAiItemInputs(request.Items));

    private static ConsumptionAiItemInput ToInput(ConsumptionAiItemHttpRequest request) =>
        new(
            request.NameEn,
            request.NameLocal,
            request.Amount,
            request.Unit,
            request.Calories,
            request.Proteins,
            request.Fats,
            request.Carbs,
            request.Fiber,
            request.Alcohol,
            request.Confidence,
            request.Resolution);

    private static List<ConsumptionItemInput> ToItemInputs(IReadOnlyList<ConsumptionItemHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];

    extension(RepeatMealHttpRequest request) {
        public RepeatMealCommand ToRepeatCommand(Guid userId, Guid mealId) =>
                new(userId, mealId, request.TargetDate, request.MealType);
    }

    private static List<ConsumptionAiSessionInput> ToAiSessionInputs(IReadOnlyList<ConsumptionAiSessionHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];

    private static List<ConsumptionAiItemInput> ToAiItemInputs(IReadOnlyList<ConsumptionAiItemHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];
}
