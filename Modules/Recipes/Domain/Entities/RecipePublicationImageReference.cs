using FoodDiary.Modules.Images.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.Recipes.Domain.Entities;

public sealed class RecipePublicationImageReference {
    public Guid RevisionId { get; set; }
    public ImageAssetId AssetId { get; set; }
}
