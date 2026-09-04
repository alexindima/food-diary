using FoodDiary.Application.Abstractions.Common.Validation;
using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed class DeleteImageAssetCommandHandler(
    IImageAssetReadRepository imageAssetRepository,
    IImageAssetCleanupService cleanupService) : ICommandHandler<DeleteImageAssetCommand, Result> {
    public async Task<Result> Handle(DeleteImageAssetCommand request, CancellationToken cancellationToken) {
        Result<UserId> userIdResult = UserIdParser.Parse(
            request.UserId,
            ImageErrors.InvalidData("UserId is required."));
        if (userIdResult.IsFailure) {
            return UserIdParser.ToFailure(userIdResult);
        }

        Result<ImageAssetId> assetIdResult = ImageAssetIdParser.ParseRequired(
            request.AssetId,
            ImageErrors.InvalidData("AssetId is required."));
        if (assetIdResult.IsFailure) {
            return Result.Failure(assetIdResult.Error);
        }

        UserId userId = userIdResult.Value;
        ImageAssetId assetId = assetIdResult.Value;

        ImageAsset? asset = await imageAssetRepository.GetOwnedByIdAsync(assetId, userId, cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure(ImageErrors.NotFound(request.AssetId));
        }

        DeleteImageAssetResult cleanupResult = await cleanupService.DeleteIfUnusedAsync(assetId, cancellationToken).ConfigureAwait(false);
        if (cleanupResult.Deleted) {
            return Result.Success();
        }

        return cleanupResult.ErrorCode switch {
            "invalid" => Result.Failure(ImageErrors.InvalidData("AssetId is required.")),
            "not_found" => Result.Failure(ImageErrors.NotFound(request.AssetId)),
            "in_use" => Result.Failure(ImageErrors.InUse()),
            "storage_error" => Result.Failure(ImageErrors.StorageError()),
            _ => Result.Failure(ImageErrors.InvalidData("Failed to delete image asset.")),
        };
    }
}
