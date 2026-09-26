using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Recipes.Application.Models;

[ExcludeFromCodeCoverage]
public sealed record RecipeImageModel(Guid ImageAssetId, string ImageUrl);
