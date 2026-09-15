using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Recipes.Presentation.Requests;

public sealed record GetRecentRecipesHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumRecentItems)] int Limit = 10,
    bool IncludePublic = true);
