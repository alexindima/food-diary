using FoodDiary.Modules.Recipes.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Contracts.Common;

public sealed record RecentRecipeUsage(RecipeId RecipeId, int UsageCount, DateTime LastUsedAtUtc);
