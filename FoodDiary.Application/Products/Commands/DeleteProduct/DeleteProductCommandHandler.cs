using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.DeleteProduct;

public class DeleteProductCommandHandler(
    IProductRepository productRepository,
    IImageAssetRepository imageAssetRepository,
    IImageStorageService imageStorageService)
    : ICommandHandler<DeleteProductCommand, Result<bool>> {
    public async Task<Result<bool>> Handle(DeleteProductCommand command, CancellationToken cancellationToken) {
        if (command.UserId is null || command.UserId == UserId.Empty) {
            return Result.Failure<bool>(Errors.Authentication.InvalidToken);
        }

        var product = await productRepository.GetByIdAsync(
            command.ProductId,
            command.UserId.Value,
            includePublic: false,
            cancellationToken: cancellationToken);
        if (product is null) {
            return Result.Failure<bool>(Errors.Product.NotAccessible(command.ProductId.Value));
        }

        var assetId = product.ImageAssetId;
        await productRepository.DeleteAsync(product);

        if (assetId.HasValue) {
            await TryDeleteAssetAsync(assetId.Value, imageAssetRepository, imageStorageService, cancellationToken);
        }

        return Result.Success(true);
    }

    private static async Task TryDeleteAssetAsync(
        ImageAssetId assetId,
        IImageAssetRepository imageAssetRepository,
        IImageStorageService storageService,
        CancellationToken cancellationToken) {
        var asset = await imageAssetRepository.GetByIdAsync(assetId, cancellationToken);
        if (asset is null) {
            return;
        }

        var inUse = await imageAssetRepository.IsAssetInUse(assetId, cancellationToken);
        if (inUse) {
            return;
        }

        await storageService.DeleteAsync(asset.ObjectKey, cancellationToken);
        await imageAssetRepository.DeleteAsync(asset, cancellationToken);
    }
}
