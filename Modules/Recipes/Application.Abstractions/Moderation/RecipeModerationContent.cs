namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public sealed record RecipeModerationContent(string Text, IReadOnlyList<RecipeModerationImage> Images, Guid OwnerId = default);
