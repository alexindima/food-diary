using FoodDiary.Application.Contracts.Common.Abstractions.Persistence;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Modules.Images.Domain.ValueObjects;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Application.Services;

internal sealed class RecipeImagePromotionService(
    IImageAssetWriteRepository images,
    IImageStorageService storage,
    IImageConfirmationTransactionRunner transactionRunner,
    IImageObjectDeletionOutbox deletionOutbox,
    IUnitOfWork unitOfWork) : IRecipeImagePromotionService {
    public Task<Result> PromoteAsync(ImageAssetId assetId, UserId ownerId, CancellationToken cancellationToken) =>
        transactionRunner.ExecuteSerializedAsync(assetId, token => PromoteOwnedAsync(assetId, ownerId, token), cancellationToken);

    private async Task<Result> PromoteOwnedAsync(ImageAssetId assetId, UserId ownerId, CancellationToken cancellationToken) {
        ImageAsset? asset = await images.GetOwnedForUpdateAsync(assetId, ownerId, cancellationToken);
        if (asset is null) {
            return Result.Failure(ImageErrors.NotFound(assetId.Value));
        }

        if (!asset.IsConfirmed) {
            return Result.Failure(ImageErrors.InvalidData("Image upload has not been confirmed."));
        }

        if (!asset.IsPrivate) {
            return Result.Success();
        }

        var key = ObjectStorageKey.FromStoredValue(asset.ObjectKey);
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(30));
        ImageObjectValidationResult result;
        try {
            result = await storage.PromotePrivateImageAsync(key, deadline.Token);
        } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
            throw;
        } catch (Exception) {
            return Result.Failure(ImageErrors.StorageError());
        }

        if (result is not ImageObjectValidationResult.Valid) {
            return Result.Failure(ImageErrors.StorageError());
        }

        asset.Promote();
        await deletionOutbox.EnqueueAsync(key.ForPrivateConfirmedImage(), isConfirmed: false, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
