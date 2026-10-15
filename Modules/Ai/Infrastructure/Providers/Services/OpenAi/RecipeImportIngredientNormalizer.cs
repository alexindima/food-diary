using System.Text.RegularExpressions;
using FoodDiary.Modules.Ai.Contracts.Models;

namespace FoodDiary.Modules.Ai.Infrastructure.Providers.Services.OpenAi;

internal static partial class RecipeImportIngredientNormalizer {
    private const string Number = @"(?:\d+\s+\d+[/⁄]\d+|\d+[.,]\d+|\d+[/⁄]\d+|\d*[¼½¾⅐⅑⅒⅓⅔⅕⅖⅗⅘⅙⅚⅛⅜⅝⅞]|\d+)";
    private const string Unit = @"(?:tablespoons?|teaspoons?|tbsp\.?|tsp\.?|cups?|kilograms?|grams?|milligrams?|millilit(?:er|re)s?|lit(?:er|re)s?|ounces?|pounds?|kg|mg|ml|g|l|oz|lbs?\.?|ст\.?\s*л\.?|ч\.?\s*л\.?|кг|мг|мл|г|л|стакан(?:а|ов)?|зубчик(?:а|ов)?)";

    public static RecipeImportIngredientModel Normalize(RecipeImportIngredientModel ingredient) {
        string name = ingredient.Name.Trim();
        string? amount = ingredient.Amount?.Trim();
        if (TrySplitLine(name, out string ingredientName, out string quantity) &&
            (string.IsNullOrEmpty(amount) || Same(amount, name) || Same(amount, quantity))) {
            return ingredient with { Name = ingredientName, Amount = quantity };
        }
        if (amount is not null && TrySplitLine(amount, out ingredientName, out quantity) && Same(name, ingredientName)) {
            return ingredient with { Amount = quantity };
        }
        // Keep an uncertain source line intact in the name instead of repeating it as a quantity.
        return Same(name, amount) ? ingredient with { Amount = null } : ingredient;
    }

    private static bool TrySplitLine(string line, out string name, out string amount) {
        Match match = QuantityPrefixRegex().Match(line);
        if (!match.Success) {
            match = ToTasteSuffixRegex().Match(line);
        }
        name = match.Groups["name"].Value.Trim();
        amount = match.Groups["amount"].Value.Trim();
        // Parenthesized alternatives or numeric names need the provider/user to resolve their meaning.
        return name.Length > 0 && char.IsLetter(name[0]) && amount.Length is > 0 and <= 128;
    }

    private static bool Same(string? left, string? right) => string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    [GeneratedRegex(@"^(?<amount>" + Number + @"(?:\s*[-–—]\s*" + Number + @")?\s*" + Unit + @")\s+(?<name>.+)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex QuantityPrefixRegex();

    [GeneratedRegex(@"^(?<name>.+?)\s+(?<amount>to taste|по вкусу)$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking)]
    private static partial Regex ToTasteSuffixRegex();
}
