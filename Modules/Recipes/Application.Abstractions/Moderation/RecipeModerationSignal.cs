namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public sealed record RecipeModerationSignal(
    string Field,
    string? RequestId,
    IReadOnlyDictionary<string, bool> Categories,
    IReadOnlyDictionary<string, double> Scores,
    bool Flagged);
