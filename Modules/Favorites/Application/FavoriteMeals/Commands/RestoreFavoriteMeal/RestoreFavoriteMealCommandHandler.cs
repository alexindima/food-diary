using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteMeals.Mappings;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteMeals.Models;
using FoodDiary.Modules.Favorites.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteMeals;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Favorites.Application.FavoriteMeals.Commands.RestoreFavoriteMeal;

public sealed class RestoreFavoriteMealCommandHandler(
    IFavoriteMealWriteRepository repository,
    IFavoriteMealSourceReadService sourceReader,
    ICurrentUserAccessService access)
    : ICommandHandler<RestoreFavoriteMealCommand, Result<FavoriteMealModel>> {
    public async Task<Result<FavoriteMealModel>> Handle(RestoreFavoriteMealCommand command, CancellationToken cancellationToken) {
        Result<UserId> user = await CurrentUserAccessResolver.ResolveAsync(command.UserId, access, cancellationToken).ConfigureAwait(false);
        if (user.IsFailure) {
            return UserIdParser.ToFailure<FavoriteMealModel>(user);
        }
        Result<FavoriteMealId> id = RequiredIdParser.Parse(command.FavoriteMealId, nameof(command.FavoriteMealId),
            "Favorite meal id must not be empty.", value => new FavoriteMealId(value));
        if (id.IsFailure) {
            return RequiredIdParser.ToFailure<FavoriteMealModel, FavoriteMealId>(id);
        }
        FavoriteMeal? favorite = await repository.GetForRestoreAsync(id.Value, user.Value, cancellationToken).ConfigureAwait(false);
        if (favorite is null) {
            return Result.Failure<FavoriteMealModel>(FavoriteMealErrors.NotFound(command.FavoriteMealId));
        }
        Result<FavoriteMealSourceModel> source = await sourceReader.GetAccessibleAsync(user.Value, favorite.MealId, cancellationToken).ConfigureAwait(false);
        if (source.IsFailure) {
            return Result.Failure<FavoriteMealModel>(source.Error);
        }
        FavoriteMeal? active = await repository.GetByMealIdAsync(favorite.MealId, user.Value, cancellationToken).ConfigureAwait(false);
        if (active is not null && active.Id != favorite.Id) {
            return Result.Failure<FavoriteMealModel>(FavoriteMealErrors.AlreadyExists);
        }
        favorite.Restore();
        return Result.Success(favorite.ToModel(source.Value));
    }
}
