using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Contracts.Ai;
using FoodDiary.Domain.ValueObjects;

namespace FoodDiary.Application.Ai.Commands.CalculateFoodNutrition;

public sealed record CalculateFoodNutritionCommand(UserId UserId, IReadOnlyList<FoodVisionItem> Items)
    : IQuery<Result<FoodNutritionResponse>>;
