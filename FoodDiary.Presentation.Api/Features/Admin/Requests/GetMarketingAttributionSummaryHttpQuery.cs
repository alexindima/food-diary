using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Admin.Requests;

public sealed record GetMarketingAttributionSummaryHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumMarketingAttributionHours)] int Hours = 720);
