using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;

public sealed class GetFavoriteMealsQueryHandler(
    IFavoriteMealReadService favoriteMealReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFavoriteMealsQuery, Result<IReadOnlyList<FavoriteMealModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteMealModel>>> Handle(
        GetFavoriteMealsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            query.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return CurrentUserAccessResolver.ToFailure<IReadOnlyList<FavoriteMealModel>>(userIdResult);
        }

        UserId userId = userIdResult.Value;
        IReadOnlyList<FavoriteMealModel> favorites = await favoriteMealReadService.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(favorites);
    }
}
