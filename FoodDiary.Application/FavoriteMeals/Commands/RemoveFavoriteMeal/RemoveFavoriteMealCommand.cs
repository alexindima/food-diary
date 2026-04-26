using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;

namespace FoodDiary.Application.FavoriteMeals.Commands.RemoveFavoriteMeal;

public record RemoveFavoriteMealCommand(
    Guid? UserId,
    Guid FavoriteMealId) : ICommand<Result>, IUserRequest;
