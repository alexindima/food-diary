using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Images.Service.Contracts.Models;

public sealed record ImageAssetReadModel(ImageAssetId Id, string Url);
