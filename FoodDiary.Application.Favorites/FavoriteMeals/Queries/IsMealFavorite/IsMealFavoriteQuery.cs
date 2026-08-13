using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.FavoriteMeals.Queries.IsMealFavorite;

public record IsMealFavoriteQuery(
    Guid? UserId,
    Guid MealId) : IQuery<Result<bool>>, IUserRequest;
