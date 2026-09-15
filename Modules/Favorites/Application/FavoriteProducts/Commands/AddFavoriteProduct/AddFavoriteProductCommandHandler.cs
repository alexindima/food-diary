using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Favorites.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Favorites.Application.FavoriteProducts.Mappings;
using FoodDiary.Modules.Favorites.Contracts.FavoriteProducts.Models;
using FoodDiary.Modules.Favorites.Domain.Entities.FavoriteProducts;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Favorites.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public sealed class AddFavoriteProductCommandHandler(
    IFavoriteProductWriteRepository favoriteProductRepository,
    IFavoriteProductSourceReadService sourceReadService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<AddFavoriteProductCommand, Result<FavoriteProductModel>> {
    public async Task<Result<FavoriteProductModel>> Handle(
        AddFavoriteProductCommand command,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = await CurrentUserAccessResolver
            .ResolveAsync(command.UserId, currentUserAccessService, cancellationToken)
            .ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<FavoriteProductModel>(userIdResult);
        }

        Result<ProductId> productIdResult = RequiredIdParser.Parse(
            command.ProductId,
            nameof(command.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return RequiredIdParser.ToFailure<FavoriteProductModel, ProductId>(productIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductId productId = productIdResult.Value;
        Result<FavoriteProductSourceModel> sourceResult = await sourceReadService
            .GetAccessibleAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        if (sourceResult.IsFailure) {
            return Result.Failure<FavoriteProductModel>(sourceResult.Error);
        }

        FavoriteProductSourceModel product = sourceResult.Value;

        FavoriteProduct? existing = await favoriteProductRepository.GetByProductIdAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<FavoriteProductModel>(FavoriteProductErrors.AlreadyExists);
        }

        var favorite = FavoriteProduct.Create(userId, productId, command.Name, command.PreferredPortionAmount ?? product.DefaultPortionAmount);
        await favoriteProductRepository.AddAsync(favorite, cancellationToken).ConfigureAwait(false);

        return Result.Success(favorite.ToModel(product));
    }
}
