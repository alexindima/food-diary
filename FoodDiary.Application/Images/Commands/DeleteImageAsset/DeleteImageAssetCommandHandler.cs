using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Abstractions.Images.Common;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed class DeleteImageAssetCommandHandler(
    IImageAssetReadRepository imageAssetRepository,
    IImageAssetCleanupService cleanupService) : ICommandHandler<DeleteImageAssetCommand, Result> {
    public async Task<Result> Handle(DeleteImageAssetCommand request, CancellationToken cancellationToken) {
        if (request.UserId == Guid.Empty) {
            return Result.Failure(Errors.Image.InvalidData("UserId is required."));
        }

        if (request.AssetId == Guid.Empty) {
            return Result.Failure(Errors.Image.InvalidData("AssetId is required."));
        }

        var userId = new UserId(request.UserId);
        var assetId = new ImageAssetId(request.AssetId);

        ImageAsset? asset = await imageAssetRepository.GetByIdAsync(assetId, cancellationToken).ConfigureAwait(false);
        if (asset is null) {
            return Result.Failure(Errors.Image.NotFound(request.AssetId));
        }

        if (asset.UserId != userId) {
            return Result.Failure(Errors.Image.Forbidden());
        }

        DeleteImageAssetResult cleanupResult = await cleanupService.DeleteIfUnusedAsync(assetId, cancellationToken).ConfigureAwait(false);
        if (cleanupResult.Deleted) {
            return Result.Success();
        }

        return cleanupResult.ErrorCode switch {
            "invalid" => Result.Failure(Errors.Image.InvalidData("AssetId is required.")),
            "not_found" => Result.Failure(Errors.Image.NotFound(request.AssetId)),
            "in_use" => Result.Failure(Errors.Image.InUse()),
            "storage_error" => Result.Failure(Errors.Image.StorageError()),
            _ => Result.Failure(Errors.Image.InvalidData("Failed to delete image asset.")),
        };
    }
}
