using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Favorites.FavoriteMeals.Commands.RemoveFavoriteMeal;

public record RemoveFavoriteMealCommand(
    Guid? UserId,
    Guid FavoriteMealId) : ICommand<Result>, IUserRequest;
