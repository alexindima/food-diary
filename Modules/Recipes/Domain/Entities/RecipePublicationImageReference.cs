using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Entities;

public sealed class RecipePublicationImageReference {
    public required Guid RevisionId { get; init; }
    public ImageAssetId AssetId { get; set; }
}
