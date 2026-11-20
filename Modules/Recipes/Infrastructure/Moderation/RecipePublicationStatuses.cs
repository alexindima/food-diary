namespace FoodDiary.Modules.Recipes.Infrastructure.Moderation;

internal static class RecipePublicationStatuses {
    public const string Pending = "Pending";
    public const string NeedsReview = "NeedsReview";
    public const string ChangesRequested = "ChangesRequested";
    public const string CheckDelayed = "CheckDelayed";
    public const string Approved = "Approved";
    public const string PhotoPromotionUnavailable = "PhotoPromotionUnavailable";
    public const string Published = "Published";
    public const string Superseded = "Superseded";

    public static string ForDisplay(string status) => status is Approved ? Pending : status;
}
