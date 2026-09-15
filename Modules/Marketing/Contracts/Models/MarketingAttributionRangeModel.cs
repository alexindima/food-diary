namespace FoodDiary.Modules.Marketing.Contracts.Models;

public sealed record MarketingAttributionRangeModel(DateTime FromUtc, DateTime ToUtc, DateTime PreviousFromUtc,
    MarketingAttributionSummaryModel Current, MarketingAttributionSummaryModel Previous,
    IReadOnlyList<MarketingAttributionDayModel> ByDay, int EventTotal);
