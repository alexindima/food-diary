using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Commands.RestoreFavoriteMeal;

public sealed record RestoreFavoriteMealCommand(Guid? UserId, Guid FavoriteMealId)
    : ICommand<Result<FavoriteMealModel>>, IUserRequest;
