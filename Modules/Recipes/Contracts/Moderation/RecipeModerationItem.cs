namespace FoodDiary.Modules.Recipes.Contracts.Moderation;

public sealed record RecipeModerationItem(
    Guid RevisionId, Guid RecipeId, Guid OwnerId, string Status, string Text, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt,
    Guid PolicyVersionId, string? Reason, string? PrimaryResult, string? ShadowResult, int Attempts,
    IReadOnlyList<RecipeModerationPreviewImage>? Images = null);
