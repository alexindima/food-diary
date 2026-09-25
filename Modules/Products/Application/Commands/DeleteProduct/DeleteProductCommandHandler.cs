using FoodDiary.Modules.Users.Contracts.Common.Validation;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Products.Application.Abstractions.Common;
using FoodDiary.Modules.Products.Contracts.Common;
using FoodDiary.Modules.Products.Application.Common;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Products.Domain.Entities;

namespace FoodDiary.Modules.Products.Application.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler(
    IProductWriteRepository productRepository,
    IProductReadRepository productReadRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService,
    IProductMutationTransactionRunner transactionRunner)
    : ICommandHandler<DeleteProductCommand, Result> {
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken) {
        Result<ProductId> productIdResult = ProductRequiredIdParser.Parse(
            command.ProductId,
            nameof(command.ProductId),
            "Product id must not be empty.",
            value => new ProductId(value));
        if (productIdResult.IsFailure) {
            return ProductRequiredIdParser.ToFailure(productIdResult);
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

        return await transactionRunner.ExecuteAsync(
            token => DeleteAsync(command, productId, userId, token),
            cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result> DeleteAsync(
        DeleteProductCommand command,
        ProductId productId,
        UserId userId,
        CancellationToken cancellationToken) {
        Product? product = await productRepository.GetByIdForUpdateAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return Result.Failure(ProductErrors.NotAccessible(command.ProductId));
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

        var galleryAssets = product.Images.Select(image => image.ImageAssetId).ToList();
        ImageAssetId? assetId = product.ImageAssetId;
        await productRepository.DeleteAsync(product, cancellationToken).ConfigureAwait(false);

        if (assetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId.Value, cancellationToken).ConfigureAwait(false);
        }

        foreach (ImageAssetId id in galleryAssets.Where(id => id != assetId)) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(id, cancellationToken).ConfigureAwait(false);
        }
        return Result.Success();
    }
}
