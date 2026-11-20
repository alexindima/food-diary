namespace FoodDiary.Modules.Recipes.Application.Abstractions.Moderation;

public sealed record RecipePublicationClaim(Guid RevisionId, Guid LeaseToken);
