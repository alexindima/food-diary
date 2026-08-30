using FoodDiary.Domain.Entities.Assets;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Common;

public sealed record ImageAssetResolution(ImageAssetId? ImageAssetId, ImageAsset? ImageAsset);
