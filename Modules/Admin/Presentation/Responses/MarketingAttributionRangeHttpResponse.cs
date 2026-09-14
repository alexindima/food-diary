namespace FoodDiary.Modules.Admin.Presentation.Responses;

public sealed record MarketingAttributionRangeHttpResponse(DateTime FromUtc, DateTime ToUtc, DateTime PreviousFromUtc,
    MarketingAttributionSummaryHttpResponse Current, MarketingAttributionSummaryHttpResponse Previous,
    IReadOnlyList<MarketingAttributionDayHttpResponse> ByDay, int EventTotal);
