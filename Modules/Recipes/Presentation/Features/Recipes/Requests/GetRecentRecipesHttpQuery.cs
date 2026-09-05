using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Presentation.Api.Features.Recipes.Requests;

public sealed record GetRecentRecipesHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int Limit = 10,
    bool IncludePublic = true);
