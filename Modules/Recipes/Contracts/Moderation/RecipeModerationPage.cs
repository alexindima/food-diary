namespace FoodDiary.Modules.Recipes.Contracts.Moderation;

public sealed record RecipeModerationPage(
    IReadOnlyList<RecipeModerationItem> Items, int TotalItems, int Page, int Limit, int PendingCount, int NeedsReviewCount, int DelayedCount);
