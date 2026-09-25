using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Ai.Presentation.Requests;

public sealed record ListFoodRecognitionsHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPage, PresentationQueryLimits.MaximumPage)] int Page = 1,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumPageSize)] int Limit = 20,
    bool? IsProductLabel = null);
