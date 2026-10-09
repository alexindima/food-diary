using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.ValueObjects;

public sealed record RecipeMediaChanges(FieldChange<string> ImageUrl, FieldChange<ImageAssetId> ImageAssetId);
