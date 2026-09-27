namespace FoodDiary.Modules.Recipes.Domain.Contracts.Enums;

public static class RecipeCategoryCodes {
    public static IReadOnlyList<string> All { get; } = Array.AsReadOnly<string>([
        "other",
        "breakfast",
        "soups",
        "salads",
        "main_courses",
        "side_dishes",
        "appetizers",
        "sandwiches",
        "pasta",
        "baking",
        "desserts",
        "drinks",
        "sauces",
        "snacks",
        "preserves",
    ]);

    public static bool IsValid(string? code) => code is not null && All.Contains(code, StringComparer.Ordinal);

    public static string ToCode(this RecipeCategory category) => category switch {
        RecipeCategory.Other => "other",
        RecipeCategory.Breakfast => "breakfast",
        RecipeCategory.Soups => "soups",
        RecipeCategory.Salads => "salads",
        RecipeCategory.MainCourses => "main_courses",
        RecipeCategory.SideDishes => "side_dishes",
        RecipeCategory.Appetizers => "appetizers",
        RecipeCategory.Sandwiches => "sandwiches",
        RecipeCategory.Pasta => "pasta",
        RecipeCategory.Baking => "baking",
        RecipeCategory.Desserts => "desserts",
        RecipeCategory.Drinks => "drinks",
        RecipeCategory.Sauces => "sauces",
        RecipeCategory.Snacks => "snacks",
        RecipeCategory.Preserves => "preserves",
        _ => throw new ArgumentOutOfRangeException(nameof(category)),
    };

    public static RecipeCategory Parse(string code) => code switch {
        "other" => RecipeCategory.Other,
        "breakfast" => RecipeCategory.Breakfast,
        "soups" => RecipeCategory.Soups,
        "salads" => RecipeCategory.Salads,
        "main_courses" => RecipeCategory.MainCourses,
        "side_dishes" => RecipeCategory.SideDishes,
        "appetizers" => RecipeCategory.Appetizers,
        "sandwiches" => RecipeCategory.Sandwiches,
        "pasta" => RecipeCategory.Pasta,
        "baking" => RecipeCategory.Baking,
        "desserts" => RecipeCategory.Desserts,
        "drinks" => RecipeCategory.Drinks,
        "sauces" => RecipeCategory.Sauces,
        "snacks" => RecipeCategory.Snacks,
        "preserves" => RecipeCategory.Preserves,
        _ => throw new ArgumentOutOfRangeException(nameof(code), "Unknown recipe category code."),
    };
}
