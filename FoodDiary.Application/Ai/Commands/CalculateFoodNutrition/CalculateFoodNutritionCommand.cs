using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Ai.Models;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed record CalculateFoodNutritionCommand(Guid UserId, IReadOnlyList<FoodVisionItemModel> Items)
    : IQuery<Result<FoodNutritionModel>>;
