using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Contracts.Consumptions;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Consumptions.Mappings;

public static class ConsumptionRequestMappings {
    public static CreateConsumptionCommand ToCommand(this CreateConsumptionRequest request, Guid? userId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
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

    public static UpdateConsumptionCommand ToCommand(this UpdateConsumptionRequest request, Guid? userId, Guid consumptionId) =>
        new(
            userId.HasValue ? new UserId(userId.Value) : null,
            new MealId(consumptionId),
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

    private static ConsumptionItemInput ToInput(ConsumptionItemRequest request) =>
        new(request.ProductId, request.RecipeId, request.Amount);

    private static ConsumptionAiSessionInput ToInput(ConsumptionAiSessionRequest request) =>
        new(
            request.ImageAssetId,
            request.RecognizedAtUtc,
            request.Notes,
            ToAiItemInputs(request.Items));

    private static ConsumptionAiItemInput ToInput(ConsumptionAiItemRequest request) =>
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
            request.Alcohol);

    private static List<ConsumptionItemInput> ToItemInputs(IReadOnlyList<ConsumptionItemRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];

    private static List<ConsumptionAiSessionInput> ToAiSessionInputs(IReadOnlyList<ConsumptionAiSessionRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];

    private static List<ConsumptionAiItemInput> ToAiItemInputs(IReadOnlyList<ConsumptionAiItemRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];
}
