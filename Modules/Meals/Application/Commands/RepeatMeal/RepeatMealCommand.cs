using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Meals.Models;

namespace FoodDiary.Application.Meals.Commands.RepeatMeal;

public record RepeatMealCommand(
    Guid? UserId,
    Guid MealId,
    DateTime TargetDate,
    string? MealType) : ICommand<Result<MealModel>>, IUserRequest;
