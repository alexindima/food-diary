namespace FoodDiary.Presentation.Api.Features.Admin.Responses;

public sealed record MarketingAttributionRangeHttpResponse(DateTime FromUtc, DateTime ToUtc, DateTime PreviousFromUtc,
    MarketingAttributionSummaryHttpResponse Current, MarketingAttributionSummaryHttpResponse Previous,
    IReadOnlyList<MarketingAttributionDayHttpResponse> ByDay, int EventTotal);
