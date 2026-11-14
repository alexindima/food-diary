namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.Recipes;

internal sealed class RecipeVideoSourceException(string reason, bool invalidSource = true) : Exception(reason) {
    public string Reason { get; } = reason;
    public bool InvalidSource { get; } = invalidSource;
}
