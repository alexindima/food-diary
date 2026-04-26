using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed record CalculateFoodNutritionCommand(Guid UserId, IReadOnlyList<FoodVisionItemModel> Items)
    : IQuery<Result<FoodNutritionModel>>;
