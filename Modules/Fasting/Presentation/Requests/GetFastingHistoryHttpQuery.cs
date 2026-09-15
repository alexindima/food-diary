using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Fasting.Presentation.Requests;

public sealed record GetFastingHistoryHttpQuery(
    DateTime From,
    DateTime To,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int Limit = 10);
