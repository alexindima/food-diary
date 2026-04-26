using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Common;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Consumptions.Commands.UpdateConsumption;

public record UpdateConsumptionCommand(
    Guid? UserId,
    Guid ConsumptionId,
    DateTime Date,
    string? MealType,
    string? Comment,
    string? ImageUrl,
    Guid? ImageAssetId,
    IReadOnlyList<ConsumptionItemInput> Items,
    IReadOnlyList<ConsumptionAiSessionInput> AiSessions,
    bool IsNutritionAutoCalculated,
    double? ManualCalories,
    double? ManualProteins,
    double? ManualFats,
    double? ManualCarbs,
    double? ManualFiber,
    double? ManualAlcohol,
    int PreMealSatietyLevel,
    int PostMealSatietyLevel) : ICommand<Result<ConsumptionModel>>, IUserRequest;
