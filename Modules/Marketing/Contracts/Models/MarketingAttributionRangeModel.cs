namespace FoodDiary.Application.Marketing.Models;

public sealed record MarketingAttributionRangeModel(DateTime FromUtc, DateTime ToUtc, DateTime PreviousFromUtc,
    MarketingAttributionSummaryModel Current, MarketingAttributionSummaryModel Previous,
    IReadOnlyList<MarketingAttributionDayModel> ByDay, int EventTotal);
