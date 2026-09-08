namespace FoodDiary.Application.Abstractions.Marketing.Common;

public sealed record MarketingAttributionRangeFilter(DateTime FromUtc, DateTime ToUtc, int Page, int Limit,
    string? EventType, string? Channel, string? Search);
