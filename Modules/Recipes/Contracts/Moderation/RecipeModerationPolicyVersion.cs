namespace FoodDiary.Modules.Recipes.Contracts.Moderation;

public sealed record RecipeModerationPolicyVersion(Guid Id, DateTimeOffset PublishedAt, Guid? PublishedBy, RecipeModerationPolicyInput Rules);
