using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.FavoriteMeals.Models;

namespace FoodDiary.Application.FavoriteMeals.Commands.AddFavoriteMeal;

public record AddFavoriteMealCommand(
    Guid? UserId,
    Guid MealId,
    string? Name) : ICommand<Result<FavoriteMealModel>>, IUserRequest;
