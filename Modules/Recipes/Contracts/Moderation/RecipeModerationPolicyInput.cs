namespace FoodDiary.Modules.Recipes.Contracts.Moderation;

public sealed record RecipeModerationPolicyInput(
    IReadOnlyList<string> TextReviewCategories, IReadOnlyList<string> ImageReviewCategories, int MistralSamplePercent);
