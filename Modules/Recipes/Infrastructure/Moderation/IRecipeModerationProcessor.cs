namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

public interface IRecipeModerationProcessor {
    Task<bool> ProcessNextAsync(bool observation, CancellationToken cancellationToken);
}
