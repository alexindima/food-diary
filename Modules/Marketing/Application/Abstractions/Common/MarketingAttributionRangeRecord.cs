namespace FoodDiary.Application.Abstractions.Marketing.Common;

public sealed record MarketingAttributionRangeRecord(MarketingAttributionSummaryRecord Current,
    MarketingAttributionSummaryRecord Previous, IReadOnlyList<MarketingAttributionDayRecord> ByDay, int EventTotal);
