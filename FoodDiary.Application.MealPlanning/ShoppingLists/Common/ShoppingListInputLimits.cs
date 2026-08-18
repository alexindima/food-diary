namespace FoodDiary.Application.MealPlanning.ShoppingLists.Common;

internal static class ShoppingListInputLimits {
    public const int NameMaxLength = 128;
    public const int ItemsMaxCount = 500;
    public const int ItemNameMaxLength = 256;
    public const int CategoryMaxLength = 128;
    public const int NoteMaxLength = 512;
    public const double AmountMaxValue = 1_000_000d;
    public const string AmountRangeErrorMessage = "Amount must be in range (0, 1000000].";
}
