using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecentItems.Common;

public sealed record RecentRecipeUsage(RecipeId RecipeId, int UsageCount, DateTime LastUsedAtUtc);
