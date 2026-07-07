using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.FavoriteProducts.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.FavoriteProducts.Mappings;
using FoodDiary.Application.FavoriteProducts.Models;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.Entities.FavoriteProducts;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.FavoriteProducts.Commands.AddFavoriteProduct;

public sealed class AddFavoriteProductCommandHandler(
    IFavoriteProductWriteRepository favoriteProductRepository,
    IProductLookupService productLookupService,
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

        UserId userId = userIdResult.Value;
        var productId = new ProductId(command.ProductId);
        IReadOnlyDictionary<ProductId, Product> products = await productLookupService
            .GetAccessibleByIdsAsync([productId], userId, cancellationToken)
            .ConfigureAwait(false);
        Product? product = products.GetValueOrDefault(productId);
        if (product is null) {
            return Result.Failure<FavoriteProductModel>(Errors.Product.NotFound(command.ProductId));
        }

        FavoriteProduct? existing = await favoriteProductRepository.GetByProductIdAsync(productId, userId, cancellationToken).ConfigureAwait(false);
        if (existing is not null) {
            return Result.Failure<FavoriteProductModel>(Errors.FavoriteProduct.AlreadyExists);
        }

        var favorite = FavoriteProduct.Create(userId, productId, command.Name, command.PreferredPortionAmount ?? product.DefaultPortionAmount);
        await favoriteProductRepository.AddAsync(favorite, cancellationToken).ConfigureAwait(false);

        return Result.Success(favorite.ToModel(product));
    }
}
