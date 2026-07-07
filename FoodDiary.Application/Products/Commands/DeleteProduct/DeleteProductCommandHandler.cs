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
    IImageAssetCleanupService imageAssetCleanupService,
    ICurrentUserAccessService currentUserAccessService)
    : ICommandHandler<DeleteProductCommand, Result> {
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken) {
        if (command.ProductId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.ProductId), "Product id must not be empty."));
        }

        Result<UserId> userIdResult = await CurrentUserAccessResolver.ResolveAsync(
            command.UserId,
            currentUserAccessService,
            cancellationToken).ConfigureAwait(false);
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        UserId userId = userIdResult.Value;
        var productId = new ProductId(command.ProductId);

        Product? product = await productRepository.GetByIdForUpdateAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        ImageAssetId? assetId = product.ImageAssetId;
        await productRepository.DeleteAsync(product, cancellationToken).ConfigureAwait(false);

        if (assetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId.Value, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
