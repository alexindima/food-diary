using System.Diagnostics.CodeAnalysis;

namespace FoodDiary.Modules.Recipes.Contracts.Models;

[ExcludeFromCodeCoverage]
public sealed record RecipeImageReadItem(Guid ImageAssetId, string ImageUrl);
