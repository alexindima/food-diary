namespace FoodDiary.Modules.Marketing.Application.Abstractions.Common;

public sealed record MarketingAttributionRangeFilter(DateTime FromUtc, DateTime ToUtc, int Page, int Limit,
    string? EventType, string? Channel, string? Search);
