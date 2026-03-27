using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Images.Common;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed class DeleteImageAssetCommandHandler(
    IImageAssetRepository imageAssetRepository,
    IImageAssetCleanupService cleanupService) : IRequestHandler<DeleteImageAssetCommand, Result> {
    public async Task<Result> Handle(DeleteImageAssetCommand request, CancellationToken cancellationToken) {
        if (request.UserId == Guid.Empty || request.AssetId == Guid.Empty) {
            return Result.Failure(Errors.Image.InvalidData("UserId and AssetId are required."));
        }

        var userId = new UserId(request.UserId);
        var assetId = new ImageAssetId(request.AssetId);

        var asset = await imageAssetRepository.GetByIdAsync(assetId, cancellationToken);
        if (asset is null) {
            return Result.Failure(Errors.Image.NotFound(request.AssetId));
        }

        if (asset.UserId != userId) {
            return Result.Failure(Errors.Image.Forbidden());
        }

        var cleanupResult = await cleanupService.DeleteIfUnusedAsync(assetId, cancellationToken);
        if (cleanupResult.Deleted) {
            return Result.Success();
        }

        return cleanupResult.ErrorCode switch {
            "invalid" => Result.Failure(Errors.Image.InvalidData("AssetId is required.")),
            "not_found" => Result.Failure(Errors.Image.NotFound(request.AssetId)),
            "in_use" => Result.Failure(Errors.Image.InUse()),
            "storage_error" => Result.Failure(Errors.Image.StorageError()),
            _ => Result.Failure(Errors.Image.InvalidData("Failed to delete image asset."))
        };
    }
}
