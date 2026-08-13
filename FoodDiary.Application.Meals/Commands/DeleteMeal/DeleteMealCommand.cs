using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Meals.Commands.DeleteMeal;

public record DeleteMealCommand(Guid? UserId, Guid MealId) : ICommand<Result>, IUserRequest;
