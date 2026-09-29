using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Requests;

public sealed record OffsetPaginationHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 20);
