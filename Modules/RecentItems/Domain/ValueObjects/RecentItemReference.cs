using FoodDiary.Domain.Primitives;
using FoodDiary.Modules.RecentItems.Domain.Enums;
using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Domain.ValueObjects;

public closed record class RecentItemReference {
    private RecentItemReference() { }

    public RecentItemType Kind => this switch {
        ProductReference => RecentItemType.Product,
        RecipeReference => RecentItemType.Recipe,
    };

    public Guid Id => this switch {
        ProductReference product => product.ProductId.Value,
        RecipeReference recipe => recipe.RecipeId.Value,
    };

    public static RecentItemReference ForProduct(ProductId id) => new ProductReference(id);
    public static RecentItemReference ForRecipe(RecipeId id) => new RecipeReference(id);

    public static RecentItemReference FromFields(RecentItemType itemType, Guid itemId) {
        DomainGuard.Defined(itemType, nameof(itemType));
        return itemType == RecentItemType.Product
            ? ForProduct(new ProductId(itemId))
            : ForRecipe(new RecipeId(itemId));
    }

    private sealed record ProductReference(ProductId ProductId) : RecentItemReference;
    private sealed record RecipeReference(RecipeId RecipeId) : RecentItemReference;
}
