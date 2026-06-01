using FoodDiary.Domain.ValueObjects.Ids;

namespace FoodDiary.Application.Abstractions.RecentItems.Common;

public sealed record RecentProductUsage(ProductId ProductId, int UsageCount, DateTime LastUsedAtUtc);
