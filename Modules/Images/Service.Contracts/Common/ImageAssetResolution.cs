using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Models;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public union ImageAssetResolution(ImageAssetResolution.Resolved, ImageAssetResolution.Unresolved) {
    public ImageAssetId? ImageAssetId => this switch {
        Resolved resolved => resolved.RequestedId,
        Unresolved unresolved => unresolved.RequestedId,
        null => null,
    };

    public ImageAssetReadModel? ImageAsset => this is Resolved resolved ? resolved.Asset : null;

    public sealed record Resolved(ImageAssetId? RequestedId, ImageAssetReadModel Asset);
    public sealed record Unresolved(ImageAssetId? RequestedId);
}
