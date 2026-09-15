using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Products.Presentation.Requests;

public sealed record GetRecentProductsHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int Limit = 10,
    bool IncludePublic = true);
