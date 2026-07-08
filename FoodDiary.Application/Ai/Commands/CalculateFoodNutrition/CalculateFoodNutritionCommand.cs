using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Ai.Models;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed record CalculateFoodNutritionCommand(Guid UserId, IReadOnlyList<FoodVisionItemModel> Items)
    : ICommand<Result<FoodNutritionModel>>;
