namespace FoodDiary.Presentation.Api.Policies;

public static class PresentationQueryLimits {
    public const int MaximumSearchLength = 128;
    public const int MaximumCategoryLength = 64;
    public const int MaximumFilterLength = 64;
    public const int MaximumCsvFilterLength = 256;
    public const int MaximumLocaleLength = 10;
    public const int MaximumSortLength = 32;
}
