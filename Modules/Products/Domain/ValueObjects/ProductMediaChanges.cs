using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Products.Domain.ValueObjects;

public sealed record ProductMediaChanges(FieldChange<string> ImageUrl, FieldChange<ImageAssetId> ImageAssetId);
