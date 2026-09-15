using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Commands.AddFavoriteMeal;

public record AddFavoriteMealCommand(
    Guid? UserId,
    Guid MealId,
    string? Name) : ICommand<Result<FavoriteMealModel>>, IUserRequest;
