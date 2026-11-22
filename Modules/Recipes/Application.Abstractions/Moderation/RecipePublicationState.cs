namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public sealed record RecipePublicationState(Guid RevisionId, string Status, string? Reason, string? ChangeJson = null);
