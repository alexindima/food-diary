using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Recipes.Presentation.Responses;

[ExcludeFromCodeCoverage]
public sealed record RecipeImageHttpResponse(Guid ImageAssetId, string ImageUrl);
