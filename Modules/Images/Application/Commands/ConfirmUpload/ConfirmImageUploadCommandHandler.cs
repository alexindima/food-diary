using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Modules.Images.Application.Abstractions.Common;
using FoodDiary.Modules.Images.Service.Contracts.Common;
using FoodDiary.Modules.Images.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Images.Application.Commands.ConfirmUpload;

public sealed class ConfirmImageUploadCommandHandler(
    IImageAssetWriteRepository imageAssetRepository,
    IImageStorageService imageStorageService,
    IImageObjectDeletionOutbox deletionOutbox,
    IUnitOfWork unitOfWork,
    IImageConfirmationTransactionRunner transactionRunner) : ICommandHandler<ConfirmImageUploadCommand, Result<ConfirmImageUploadResult>> {
    public async Task<Result<ConfirmImageUploadResult>> Handle(
        ConfirmImageUploadCommand request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(request.UserId, ImageErrors.InvalidData("UserId is required."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<ConfirmImageUploadResult>(userIdResult);
        }

        Result<ImageAssetId> assetIdResult = ImageAssetIdParser.ParseRequired(
            request.AssetId,
            ImageErrors.InvalidData("AssetId is required."));
        if (assetIdResult.IsFailure) {
            return Result.Failure<ConfirmImageUploadResult>(assetIdResult.Error);
        }

        return await transactionRunner.ExecuteSerializedAsync(assetIdResult.Value,
            token => ConfirmAsync(assetIdResult.Value, userIdResult.Value, token), cancellationToken).ConfigureAwait(false);
    }

    private async Task<Result<ConfirmImageUploadResult>> ConfirmAsync(ImageAssetId assetId, UserId userId, CancellationToken cancellationToken) {
        ImageAsset? asset = await imageAssetRepository.GetOwnedForUpdateAsync(
            assetId,
            userId,
            cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure<ConfirmImageUploadResult>(ImageErrors.NotFound(assetId.Value));
        }

        if (!asset.IsConfirmed) {
            ImageObjectValidationResult validation;
            try {
                validation = await imageStorageService
                    .ConfirmUploadedObjectAsync(asset.ObjectKey, cancellationToken)
                    .ConfigureAwait(false);
            } catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
                throw;
            } catch (Exception) {
                return Result.Failure<ConfirmImageUploadResult>(ImageErrors.StorageError());
            }

            if (!validation.IsValid) {
                return Result.Failure<ConfirmImageUploadResult>(ImageErrors.InvalidData(
                    validation.Message ?? "Image upload has not completed or is invalid."));
            }

            asset.Confirm();
            await deletionOutbox.EnqueueAsync(asset.ObjectKey, isConfirmed: false, cancellationToken).ConfigureAwait(false);
            await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            // Never delete the published object on persistence failure: commit may have succeeded.
            // Pending assets are reclaimed by orphan cleanup, which handles both buckets.
        }

        return Result.Success(new ConfirmImageUploadResult(asset.Id.Value, asset.Url));
    }
}
