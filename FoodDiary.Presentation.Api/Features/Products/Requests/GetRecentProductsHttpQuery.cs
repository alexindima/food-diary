using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Products.Requests;

public sealed record GetRecentProductsHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int Limit = 10,
    bool IncludePublic = true);
