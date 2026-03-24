using FoodDiary.Application.Common.Abstractions.Result;
using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using FoodDiary.Domain.ValueObjects.Ids;
using MediatR;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed class DeleteImageAssetCommandHandler(
    IImageAssetRepository imageAssetRepository,
    IImageAssetCleanupService cleanupService) : IRequestHandler<DeleteImageAssetCommand, Result> {
    public async Task<Result> Handle(DeleteImageAssetCommand request, CancellationToken cancellationToken) {
        if (request.UserId == UserId.Empty || request.AssetId == ImageAssetId.Empty) {
            return Result.Failure(Errors.Image.InvalidData("UserId and AssetId are required."));
        }

        var asset = await imageAssetRepository.GetByIdAsync(request.AssetId, cancellationToken);
        if (asset is null) {
            return Result.Failure(Errors.Image.NotFound(request.AssetId.Value));
        }

        if (asset.UserId != request.UserId) {
            return Result.Failure(Errors.Image.Forbidden());
        }

        var cleanupResult = await cleanupService.DeleteIfUnusedAsync(request.AssetId, cancellationToken);
        if (cleanupResult.Deleted) {
            return Result.Success();
        }

        return cleanupResult.ErrorCode switch {
            "invalid" => Result.Failure(Errors.Image.InvalidData("AssetId is required.")),
            "not_found" => Result.Failure(Errors.Image.NotFound(request.AssetId.Value)),
            "in_use" => Result.Failure(Errors.Image.InUse()),
            "storage_error" => Result.Failure(Errors.Image.StorageError()),
            _ => Result.Failure(Errors.Image.InvalidData("Failed to delete image asset."))
        };
    }
}
