using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Interfaces.Persistence;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Products.Mappings;
using FoodDiary.Application.Products.Models;
using FoodDiary.Domain.Entities.Products;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Products.Commands.UpdateProduct;

public class UpdateProductCommandHandler(
    IProductRepository productRepository,
    IImageAssetCleanupService imageAssetCleanupService,
    IUserRepository userRepository,
    IImageAssetAccessService imageAssetAccessService)
    : ICommandHandler<UpdateProductCommand, Result<ProductModel>> {
    public async Task<Result<ProductModel>>
        Handle(UpdateProductCommand command, CancellationToken cancellationToken) {
        Result<ProductUpdateValues> valuesResult = await ProductUpdateValuePreparer.PrepareAsync(
            command,
            userRepository,
            imageAssetAccessService,
            cancellationToken).ConfigureAwait(false);
        if (valuesResult.IsFailure) {
            return Result.Failure<ProductModel>(valuesResult.Error);
        }

        ProductUpdateValues values = valuesResult.Value;
        Product? product = await productRepository.GetByIdForUpdateAsync(
            values.ProductId,
            values.UserId,
            includePublic: false,
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (product is null) {
            return Result.Failure<ProductModel>(Errors.Product.NotAccessible(command.ProductId));
        }

        ImageAssetId? oldAssetId = product.ImageAssetId;
        DateTime? modifiedOnBefore = product.ModifiedOnUtc;
        ProductUpdateApplier.Apply(product, command, values);

        bool hasChanges = product.ModifiedOnUtc != modifiedOnBefore;
        if (hasChanges) {
            await productRepository.UpdateAsync(product, cancellationToken).ConfigureAwait(false);
        }

        await CleanupOldImageAssetAsync(
            oldAssetId,
            command,
            hasChanges,
            cancellationToken).ConfigureAwait(false);

        int usageCount = product.MealItems.Count + product.RecipeIngredients.Count;
        return Result.Success(product.ToModel(usageCount, isOwnedByCurrentUser: true));
    }

    private async Task CleanupOldImageAssetAsync(
        ImageAssetId? oldAssetId,
        UpdateProductCommand command,
        bool hasChanges,
        CancellationToken cancellationToken) {
        bool imageAssetChanged = command.ClearImageAssetId ||
                                (command.ImageAssetId.HasValue && (!oldAssetId.HasValue || oldAssetId.Value.Value != command.ImageAssetId.Value));

        if (hasChanges && oldAssetId.HasValue && imageAssetChanged) {
            await imageAssetCleanupService.DeleteIfUnusedAsync(oldAssetId.Value, cancellationToken).ConfigureAwait(false);
        }
    }
}
