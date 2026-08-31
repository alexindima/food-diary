using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Meals.Common;
using FoodDiary.Application.Meals.Models;

namespace FoodDiary.Application.Meals.Commands.CreateMeal;

public record CreateMealCommand(
    Guid? UserId,
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<MealItemInput> Items,
    IReadOnlyList<MealAiSessionInput> AiSessions,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    int PreMealSatietyLevel,
    int PostMealSatietyLevel) : ICommand<Result<MealModel>>, IUserRequest;
