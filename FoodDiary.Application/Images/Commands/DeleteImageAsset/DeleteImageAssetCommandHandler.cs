using FoodDiary.Application.Common.Interfaces.Persistence;
using FoodDiary.Application.Common.Interfaces.Services;
using MediatR;

namespace FoodDiary.Application.Images.Commands.DeleteImageAsset;

public sealed class DeleteImageAssetCommandHandler(
    IImageAssetRepository imageAssetRepository,
    IImageAssetCleanupService cleanupService) : IRequestHandler<DeleteImageAssetCommand, DeleteImageAssetResult>
{
    public async Task<DeleteImageAssetResult> Handle(DeleteImageAssetCommand request, CancellationToken cancellationToken)
    {
        var asset = await imageAssetRepository.GetByIdAsync(request.AssetId, cancellationToken);
        if (asset is null)
        {
            return new DeleteImageAssetResult(false, "not_found");
        }

        if (asset.UserId != request.UserId)
        {
            return new DeleteImageAssetResult(false, "forbidden");
        }

        return await cleanupService.DeleteIfUnusedAsync(request.AssetId, cancellationToken);
    }
}
