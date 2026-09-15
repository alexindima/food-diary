using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Meals.Service.Contracts.Models;

namespace FoodDiary.Modules.Meals.Application.Commands.RepeatMeal;

public record RepeatMealCommand(
    Guid? UserId,
    Guid MealId,
    DateTime TargetDate,
    string? MealType) : ICommand<Result<MealModel>>, IUserRequest;
