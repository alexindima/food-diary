using FoodDiary.Application.Consumptions.Commands.CreateConsumption;
using FoodDiary.Application.Consumptions.Commands.DeleteConsumption;
using FoodDiary.Application.Consumptions.Commands.RepeatMeal;
using FoodDiary.Application.Consumptions.Commands.UpdateConsumption;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Presentation.Api.Features.Consumptions.Requests;

namespace FoodDiary.Presentation.Api.Features.Consumptions.Mappings;

public static class ConsumptionHttpMappings {
    public static DeleteConsumptionCommand ToDeleteCommand(this Guid consumptionId, Guid userId) =>
        new(userId, consumptionId);

    public static CreateConsumptionCommand ToCommand(this CreateConsumptionHttpRequest request, Guid userId) =>
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

    public static UpdateConsumptionCommand ToCommand(this UpdateConsumptionHttpRequest request, Guid userId, Guid consumptionId) =>
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

    private static ConsumptionItemInput ToInput(ConsumptionItemHttpRequest request) =>
        new(request.ProductId, request.RecipeId, request.Amount);

    private static ConsumptionAiSessionInput ToInput(ConsumptionAiSessionHttpRequest request) =>
        new(
            request.ImageAssetId,
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
            request.Alcohol);

    private static List<ConsumptionItemInput> ToItemInputs(IReadOnlyList<ConsumptionItemHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];

    public static RepeatMealCommand ToRepeatCommand(this RepeatMealHttpRequest request, Guid userId, Guid mealId) =>
        new(userId, mealId, request.TargetDate, request.MealType);

    private static List<ConsumptionAiSessionInput> ToAiSessionInputs(IReadOnlyList<ConsumptionAiSessionHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];

    private static List<ConsumptionAiItemInput> ToAiItemInputs(IReadOnlyList<ConsumptionAiItemHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];
}
