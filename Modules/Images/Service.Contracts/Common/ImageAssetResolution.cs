using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Images.Service.Contracts.Models;

namespace FoodDiary.Modules.Images.Service.Contracts.Common;

public sealed record ImageAssetResolution(ImageAssetId? ImageAssetId, ImageAssetReadModel? ImageAsset);
