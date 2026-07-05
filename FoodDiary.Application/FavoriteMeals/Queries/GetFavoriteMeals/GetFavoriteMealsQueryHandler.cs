using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;

public sealed class GetFavoriteMealsQueryHandler(
    IFavoriteMealReadService favoriteMealReadService,
    ICurrentUserAccessService currentUserAccessService)
    : IQueryHandler<GetFavoriteMealsQuery, Result<IReadOnlyList<FavoriteMealModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteMealModel>>> Handle(
        GetFavoriteMealsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<FavoriteMealModel>>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await currentUserAccessService.EnsureCanAccessAsync(userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<FavoriteMealModel>>(accessError);
        }

        IReadOnlyList<FavoriteMealModel> favorites = await favoriteMealReadService.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        return Result.Success(favorites);
    }
}
