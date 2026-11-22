namespace FoodDiary.Modules.Recipes.Contracts.Moderation;

public sealed record RecipeModerationPolicyView(
    RecipeModerationPolicyVersion Active, IReadOnlyList<RecipeModerationPolicyVersion> History, bool MistralAvailable);
