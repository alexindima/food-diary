using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Queries.IsMealFavorite;

public sealed class IsMealFavoriteQueryHandler(
    IFavoriteMealReadRepository favoriteMealRepository,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<IsMealFavoriteQuery, Result<bool>> {
    public async Task<Result<bool>> Handle(
        IsMealFavoriteQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<bool>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<bool>(accessError);
        }

        var mealId = new MealId(query.MealId);
        bool isFavorite = await favoriteMealRepository.ExistsByMealIdAsync(mealId, userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(isFavorite);
    }
}
