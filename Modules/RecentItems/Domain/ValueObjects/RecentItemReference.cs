using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.RecentItems.Domain.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Domain.ValueObjects;

public sealed record RecentItemReference {
    public RecentItemType Kind { get; }
    public Guid Id { get; }
    private RecentItemReference(RecentItemType kind, Guid id) {
        Kind = kind;
        Id = id;
    }

    public static RecentItemReference ForProduct(ProductId id) => new(RecentItemType.Product, id.Value);
    public static RecentItemReference ForRecipe(RecipeId id) => new(RecentItemType.Recipe, id.Value);

    public static RecentItemReference FromFields(RecentItemType itemType, Guid itemId) {
        DomainGuard.Defined(itemType, nameof(itemType));
        return itemType switch {
            RecentItemType.Product => ForProduct(new ProductId(itemId)),
            RecentItemType.Recipe => ForRecipe(new RecipeId(itemId)),
            _ => throw new ArgumentOutOfRangeException(nameof(itemType)),
        };
    }
}
