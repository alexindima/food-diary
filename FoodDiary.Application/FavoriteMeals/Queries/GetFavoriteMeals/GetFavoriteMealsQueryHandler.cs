using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteMeals.Common;
using FoodDiary.Application.FavoriteMeals.Mappings;
using FoodDiary.Application.FavoriteMeals.Models;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.FavoriteMeals.Queries.GetFavoriteMeals;

public class GetFavoriteMealsQueryHandler(
    IFavoriteMealRepository favoriteMealRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetFavoriteMealsQuery, Result<IReadOnlyList<FavoriteMealModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteMealModel>>> Handle(
        GetFavoriteMealsQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<FavoriteMealModel>>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<FavoriteMealModel>>(accessError);
        }

        var favorites = await favoriteMealRepository.GetAllAsync(userId, cancellationToken);
        var models = favorites.Select(f => f.ToModel()).ToList();
        return Result.Success<IReadOnlyList<FavoriteMealModel>>(models);
    }
}
