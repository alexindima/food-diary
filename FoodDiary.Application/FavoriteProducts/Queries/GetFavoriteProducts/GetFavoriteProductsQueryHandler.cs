using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.FavoriteProducts.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.Users.Common;

namespace FoodDiary.Application.FavoriteProducts.Queries.GetFavoriteProducts;

public class GetFavoriteProductsQueryHandler(
    IFavoriteProductRepository favoriteProductRepository,
    IUserRepository userRepository)
    : IQueryHandler<GetFavoriteProductsQuery, Result<IReadOnlyList<FavoriteProductModel>>> {
    public async Task<Result<IReadOnlyList<FavoriteProductModel>>> Handle(
        GetFavoriteProductsQuery query,
        CancellationToken cancellationToken) {
        var userIdResult = UserIdParser.Parse(query.UserId);
        if (userIdResult.IsFailure) {
            return Result.Failure<IReadOnlyList<FavoriteProductModel>>(userIdResult.Error);
        }

        var userId = userIdResult.Value;
        var accessError = await CurrentUserAccessLoader.EnsureCanAccessAsync(userRepository, userId, cancellationToken);
        if (accessError is not null) {
            return Result.Failure<IReadOnlyList<FavoriteProductModel>>(accessError);
        }

        var favorites = await favoriteProductRepository.GetAllAsync(userId, cancellationToken);
        var models = favorites.Select(f => f.ToModel()).ToList();
        return Result.Success<IReadOnlyList<FavoriteProductModel>>(models);
    }
}
