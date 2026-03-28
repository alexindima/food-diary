using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IImageAssetCleanupService imageAssetCleanupService)
    : ICommandHandler<DeleteProductCommand, Result> {
    public async Task<Result> Handle(DeleteProductCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == Guid.Empty) {
            return Result.Failure(Errors.Authentication.InvalidToken);
        }

        if (command.ProductId == Guid.Empty) {
            return Result.Failure(Errors.Validation.Invalid(nameof(command.ProductId), "Product id must not be empty."));
        }

        var userId = new UserId(command.UserId!.Value);
        var productId = new ProductId(command.ProductId);

        var product = await productRepository.GetByIdAsync(
            productId,
            userId,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure(Errors.Product.NotAccessible(command.ProductId));
        }

        var assetId = product.ImageAssetId;
        await productRepository.DeleteAsync(product, cancellationToken);

        if (assetId.HasValue) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(assetId.Value, cancellationToken);
        }

        return Result.Success();
    }
}
