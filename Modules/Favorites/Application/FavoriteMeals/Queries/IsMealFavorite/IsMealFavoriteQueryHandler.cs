using FoodDiary.Modules.Meals.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadMealFavoriteStatus;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.IsMealFavorite;

public sealed class IsMealFavoriteQueryHandler(
    ISender sender,
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

        Result<MealId> mealIdResult = RequiredIdParser.Parse(
            query.MealId,
            nameof(query.MealId),
            "Meal id must not be empty.",
            value => new MealId(value));
        if (mealIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<bool, MealId>(mealIdResult);
        }

        UserId userId = userIdResult.Value;
        MealId mealId = mealIdResult.Value;
        bool isFavorite = await sender.Send(new ReadMealFavoriteStatusQuery(mealId, userId), cancellationToken).ConfigureAwait(false);
        return Result.Success(isFavorite);
    }
}
