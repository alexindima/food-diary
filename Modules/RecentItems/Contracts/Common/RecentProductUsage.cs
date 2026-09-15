using FoodDiary.Modules.Products.Domain.Contracts.ValueObjects.Ids;

namespace FoodDiary.Modules.RecentItems.Contracts.Common;

public sealed record RecentProductUsage(ProductId ProductId, int UsageCount, DateTime LastUsedAtUtc);
