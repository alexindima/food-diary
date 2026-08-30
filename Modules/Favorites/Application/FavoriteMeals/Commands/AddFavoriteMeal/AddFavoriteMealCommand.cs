using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.FavoriteMeals.Models;

namespace FoodDiary.Application.Favorites.FavoriteMeals.Commands.AddFavoriteMeal;

public record AddFavoriteMealCommand(
    Guid? UserId,
    Guid MealId,
    string? Name) : ICommand<Result<FavoriteMealModel>>, IUserRequest;
