namespace FoodDiary.Modules.Marketing.Application.Abstractions.Common;

public sealed record MarketingAttributionRangeRecord(MarketingAttributionSummaryRecord Current,
    MarketingAttributionSummaryRecord Previous, IReadOnlyList<MarketingAttributionDayRecord> ByDay, int EventTotal);
