using FoodDiary.Application.Meals.Commands.CreateMeal;
using FoodDiary.Application.Meals.Commands.DeleteMeal;
using FoodDiary.Application.Meals.Commands.RepeatMeal;
using FoodDiary.Application.Meals.Commands.UpdateMeal;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Presentation.Api.Features.Meals.Requests;

namespace FoodDiary.Presentation.Api.Features.Meals.Mappings;

public static class MealHttpMappings {
    extension(Guid mealId) {
        public DeleteMealCommand ToDeleteCommand(Guid userId) =>
                new(userId, mealId);
    }

    extension(CreateMealHttpRequest request) {
        public CreateMealCommand ToCommand(Guid userId) =>
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

    extension(UpdateMealHttpRequest request) {
        public UpdateMealCommand ToCommand(Guid userId, Guid mealId) =>
                new(
                    userId,
                    mealId,
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

    private static MealItemInput ToInput(MealItemHttpRequest request) =>
        new(request.ProductId, request.RecipeId, request.Amount, request.SourceAiItemId, request.Origin);

    private static MealAiSessionInput ToInput(MealAiSessionHttpRequest request) =>
        new(
            request.ImageAssetId,
            request.Source,
            request.RecognizedAtUtc,
            request.Notes,
            ToAiItemInputs(request.Items));

    private static MealAiItemInput ToInput(MealAiItemHttpRequest request) =>
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

    private static List<MealItemInput> ToItemInputs(IReadOnlyList<MealItemHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];

    extension(RepeatMealHttpRequest request) {
        public RepeatMealCommand ToRepeatCommand(Guid userId, Guid mealId) =>
                new(userId, mealId, request.TargetDate, request.MealType);
    }

    private static List<MealAiSessionInput> ToAiSessionInputs(IReadOnlyList<MealAiSessionHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];

    private static List<MealAiItemInput> ToAiItemInputs(IReadOnlyList<MealAiItemHttpRequest>? requests) =>
        requests?.Select(ToInput).ToList() ?? [];
}
