using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.FavoriteMeals;

namespace FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;

public class GetFavoriteMealsQueryHandler(
    IFavoriteMealRepository favoriteMealRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetFavoriteMealsQuery, Result<IReadOnlyList<FavoriteMealModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteMealModel>>> Handle(
        GetFavoriteMealsQuery query,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<FavoriteMealModel>>(userIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        Error? accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken).ConfigureAwait(false);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<FavoriteMealModel>>(accessError);
        }

        IReadOnlyList<FavoriteMeal> favorites = await favoriteMealRepository.GetAllAsync(userId, cancellationToken).ConfigureAwait(false);
        var models = favorites.Select(f => f.ToModel()).ToList();
        return Result.Success<IReadOnlyList<FavoriteMealModel>>(models);
    }
}
