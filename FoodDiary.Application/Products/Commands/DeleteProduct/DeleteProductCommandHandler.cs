using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Validation;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Products.Common;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Users.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Domain.Entities.Products;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IProductWriteRepository productRepository,
    IProductReadRepository productReadRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteProductCommand, Result> {
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken) {
        Result<ProductId> productIdResult = RequiredIdParser.Parse(
            command.ProductId,
            nameof(command.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return RequiredIdParser.ToFailure(productIdResult);
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        ProductId productId = productIdResult.Value;

        Product? product = await productRepository.GetByIdForUpdateAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        int usageCount = await productReadRepository.GetUsageCountAsync(
            product.Id,
            product.UserId,
            includePublic: false,
            cancellationToken).ConfigureAwait(false);
        if (usageCount > 0) {
            return Result.Failure(Errors.Validation.Invalid(
                nameof(command.ProductId),
                "Product is already used and cannot be deleted"));
        }

        ImageAssetId? assetId = product.ImageAssetId;
        await productRepository.DeleteAsync(product, cancellationToken).ConfigureAwait(false);

        if (assetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId.Value, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
