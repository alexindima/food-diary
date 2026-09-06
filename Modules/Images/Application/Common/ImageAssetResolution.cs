using FoodDiary.Application.Abstractions.Images.Models;
using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Images.Common;

public sealed record ImageAssetResolution(ImageAssetId? ImageAssetId, ImageAssetReadModel? ImageAsset);
