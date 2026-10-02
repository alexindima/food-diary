using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Dietologist.Presentation.Requests;

public sealed record GetRecommendationsHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 50);
