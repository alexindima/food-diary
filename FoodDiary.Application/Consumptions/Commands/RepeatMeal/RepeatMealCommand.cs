using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Consumptions.Models;

namespace FoodDiary.Application.Consumptions.Commands.RepeatMeal;

public record RepeatMealCommand(
    Guid? UserId,
    Guid MealId,
    DateTime TargetDate,
    string? MealType) : ICommand<Result<ConsumptionModel>>, IUserRequest;
