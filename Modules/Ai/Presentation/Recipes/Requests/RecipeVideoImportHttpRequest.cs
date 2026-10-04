namespace FoodDiary.Modules.Ai.Presentation.Recipes.Requests;

public sealed record RecipeVideoImportHttpRequest(string? SourceUrl = null, string? Text = null);
