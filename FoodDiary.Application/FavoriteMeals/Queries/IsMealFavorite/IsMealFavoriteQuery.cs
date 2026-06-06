using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;

namespace FoodDiary.Application.FavoriteMeals.Queries.IsMealFavorite;

public record IsMealFavoriteQuery(
    Guid? UserId,
    Guid MealId) : IQuery<Result<bool>>, IUserRequest;
