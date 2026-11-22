namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public interface IRecipeModerationProvider {
    string Name { get; }
    bool IsAvailable { get; }

    Task<RecipeModerationCheck> CheckAsync(RecipeModerationContent content, CancellationToken cancellationToken);
}
