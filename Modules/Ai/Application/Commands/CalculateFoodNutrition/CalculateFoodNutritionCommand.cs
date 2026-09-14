using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Application.Commands.CalculateFoodNutrition;

public sealed record CalculateFoodNutritionCommand(Guid UserId, IReadOnlyList<FoodVisionItemModel> Items, string RequestId)
    : ICommand<Result<FoodNutritionModel>>;
