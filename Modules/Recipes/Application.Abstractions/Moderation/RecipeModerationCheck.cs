namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public sealed record RecipeModerationCheck(string Model, IReadOnlyList<RecipeModerationSignal> Signals);
