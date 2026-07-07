using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteMeals.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Queries.IsMealFavorite;

public sealed class IsMealFavoriteQueryHandler(
    IFavoriteMealReadService favoriteMealReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<IsMealFavoriteQuery, Result<bool>> {
    public async Task<Result<bool>> Handle(
        IsMealFavoriteQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<bool>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var mealId = new MealId(query.MealId);
        bool isFavorite = await favoriteMealReadService.ExistsByMealIdAsync(mealId, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(isFavorite);
    }
}
