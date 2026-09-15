using FoodDiary.Mediator;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Queries.ReadFavoriteMeals;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Queries.GetFavoriteMeals;

public sealed class GetFavoriteMealsQueryHandler(
    ISender sender,
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
        IReadOnlyList<FavoriteMealModel> favorites = await sender.Send(new ReadFavoriteMealsQuery(userId), cancellationToken).ConfigureAwait(false);
        return Result.Success(favorites);
    }
}
