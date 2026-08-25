using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Persistence;
using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Application.Images.Commands.ConfirmUpload;

public sealed class ConfirmImageUploadCommandHandler(
    IImageAssetWriteRepository imageAssetRepository,
    IImageStorageService imageStorageService,
    IImageObjectDeletionOutbox deletionOutbox,
    IUnitOfWork unitOfWork) : ICommandHandler<ConfirmImageUploadCommand, Result<ConfirmImageUploadResult>> {
    public async Task<Result<ConfirmImageUploadResult>> Handle(
        ConfirmImageUploadCommand request,
        CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(request.UserId, Errors.Image.InvalidData("UserId is required."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure<ConfirmImageUploadResult>(userIdResult);
        }

        Result<ImageAssetId> assetIdResult = ImageAssetIdParser.ParseRequired(
            request.AssetId,
            Errors.Image.InvalidData("AssetId is required."));
        if (assetIdResult.IsFailure) {
            return Result.Failure<ConfirmImageUploadResult>(assetIdResult.Error);
        }

        ImageAsset? asset = await imageAssetRepository.GetOwnedForUpdateAsync(
            assetIdResult.Value,
            userIdResult.Value,
            cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure<ConfirmImageUploadResult>(Errors.Image.NotFound(request.AssetId));
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
                return Result.Failure<ConfirmImageUploadResult>(Errors.Image.StorageError());
            }

            if (!validation.IsValid) {
                return Result.Failure<ConfirmImageUploadResult>(Errors.Image.InvalidData(
                    validation.Message ?? "Image upload has not completed or is invalid."));
            }

            try {
                asset.Confirm();
                await deletionOutbox.EnqueueAsync(asset.ObjectKey, isConfirmed: false, cancellationToken).ConfigureAwait(false);
                await unitOfWork.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            } catch {
                try {
                    await imageStorageService.DeleteAsync(
                        asset.ObjectKey,
                        isConfirmed: true,
                        CancellationToken.None).ConfigureAwait(false);
                } catch {
                    // A later orphan/user cleanup also targets both buckets for pending assets.
                }

                throw;
            }
        }

        return Result.Success(new ConfirmImageUploadResult(asset.Id.Value, asset.Url));
    }
}
