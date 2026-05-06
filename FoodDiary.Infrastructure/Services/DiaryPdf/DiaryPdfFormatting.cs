using System.Globalization;
using FoodDiary.Domain.Entities.Meals;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private static double EffectiveCalories(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalCalories : meal.ManualCalories ?? meal.TotalCalories;

    private static double EffectiveProteins(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalProteins : meal.ManualProteins ?? meal.TotalProteins;

    private static double EffectiveFats(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalFats : meal.ManualFats ?? meal.TotalFats;

    private static double EffectiveCarbs(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalCarbs : meal.ManualCarbs ?? meal.TotalCarbs;

    private static double EffectiveFiber(Meal meal) =>
        meal.IsNutritionAutoCalculated ? meal.TotalFiber : meal.ManualFiber ?? meal.TotalFiber;

    private static string FormatNumber(double value, int decimals) =>
        FormatNumber(value, decimals, CultureInfo.InvariantCulture);

    private static string FormatNumber(double value, int decimals, CultureInfo culture) =>
        Math.Round(value, decimals).ToString($"N{decimals}", culture);

    private static string ApplyAlpha(string hex, double alpha) {
        var normalized = hex.TrimStart('#');
        if (normalized.Length != 6) {
            return hex;
        }

        var alphaByte = (byte)Math.Round(Math.Clamp(alpha, 0, 1) * 255);
        return $"#{alphaByte:X2}{normalized}";
    }

    private static string ResolveReportHost(string? reportOrigin) {
        if (string.IsNullOrWhiteSpace(reportOrigin)) {
            return DefaultReportHost;
        }

        var normalized = reportOrigin.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https" &&
            !string.IsNullOrWhiteSpace(uri.Host)) {
            return FormatHost(uri.IdnHost, uri.Port, uri.IsDefaultPort);
        }

        return Uri.CheckHostName(normalized) == UriHostNameType.Unknown
            ? DefaultReportHost
            : FormatHost(normalized, port: null, isDefaultPort: true);
    }

    private static string FormatHost(string host, int? port, bool isDefaultPort) {
        var unicodeHost = ToUnicodeHost(host.Trim().TrimEnd('.'));
        return port.HasValue && !isDefaultPort
            ? $"{unicodeHost}:{port.Value}"
            : unicodeHost;
    }

    private static string ToUnicodeHost(string host) {
        try {
            return new IdnMapping().GetUnicode(host);
        } catch (ArgumentException) {
            return DefaultReportHost;
        }
    }
    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : $"{value[..Math.Max(0, maxLength - 3)]}...";

    private static string FormatMealItems(Meal meal, DiaryReportData report) {
        var items = FormatMealItemsList(meal, report);
        return items == report.Texts.ItemsNotSpecified
            ? $"{report.Texts.ItemsPrefix}: {report.Texts.ItemsNotSpecified}"
            : $"{report.Texts.ItemsPrefix}: {items}";
    }

    private static string FormatMealItemsList(Meal meal, DiaryReportData report) {
        var itemLabels = FormatMealItemLabels(meal, report, maxItems: 6);
        return itemLabels.Count == 0
            ? report.Texts.ItemsNotSpecified
            : Truncate(string.Join(", ", itemLabels), 220);
    }

    private static IReadOnlyList<string> FormatMealItemLabels(Meal meal, DiaryReportData report, int maxItems) {
        var compositionItems = GetMealCompositionItems(meal, report);
        var itemLabels = compositionItems
            .Select(item => item.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Take(maxItems)
            .ToArray();

        if (itemLabels.Length == 0) {
            return [];
        }

        var suffix = compositionItems.Count > itemLabels.Length
            ? $" +{compositionItems.Count - itemLabels.Length} {report.Texts.MoreItemsSuffix}"
            : "";

        return suffix.Length == 0
            ? itemLabels
            : [.. itemLabels, suffix.TrimStart()];
    }

    private static IReadOnlyList<MealCompositionItem> GetMealCompositionItems(Meal meal, DiaryReportData report) {
        var manualItems = meal.Items
            .OrderBy(item => item.CreatedOnUtc)
            .Select(item => FormatMealItem(item, report))
            .ToArray();

        var aiItems = meal.AiSessions
            .OrderBy(session => session.RecognizedAtUtc)
            .SelectMany(session => session.Items.OrderBy(item => item.CreatedOnUtc))
            .Select(item => FormatMealAiItem(item, report))
            .ToArray();

        return [.. manualItems, .. aiItems];
    }

    private static MealCompositionItem FormatMealItem(MealItem item, DiaryReportData report) {
        var name = item.Product?.Name ?? item.Recipe?.Name;
        if (string.IsNullOrWhiteSpace(name)) {
            name = item.IsRecipe ? report.Texts.RecipeFallback : report.Texts.ProductFallback;
        }

        var amountUnit = item.IsRecipe ? report.Texts.ServingUnit : FormatProductUnit(item, report);
        var amount = $"{FormatNumber(item.Amount, item.IsRecipe ? 1 : 0, report.Culture)} {amountUnit}";
        var nutrition = CalculateMealItemNutrition(item);

        return new MealCompositionItem(
            Label: $"{amount} {name}",
            Name: CapitalizeFirstLetter(name, report.Culture),
            Amount: amount,
            Calories: nutrition.Calories,
            Proteins: nutrition.Proteins,
            Fats: nutrition.Fats,
            Carbs: nutrition.Carbs,
            Fiber: nutrition.Fiber);
    }

    private static string FormatProductUnit(MealItem item, DiaryReportData report) =>
        FormatUnit(item.Product?.BaseUnit.ToString(), report);

    private static MealCompositionItem FormatMealAiItem(MealAiItem item, DiaryReportData report) {
        var name = ResolveAiItemName(item, report);
        var unit = FormatUnit(item.Unit, report);
        var amount = $"{FormatNumber(item.Amount, 0, report.Culture)} {unit}";

        return new MealCompositionItem(
            Label: $"{amount} {name}",
            Name: CapitalizeFirstLetter(name, report.Culture),
            Amount: amount,
            Calories: item.Calories,
            Proteins: item.Proteins,
            Fats: item.Fats,
            Carbs: item.Carbs,
            Fiber: item.Fiber);
    }

    private static string ResolveAiItemName(MealAiItem item, DiaryReportData report) {
        if (report.Culture.TwoLetterISOLanguageName != "en" && !string.IsNullOrWhiteSpace(item.NameLocal)) {
            return item.NameLocal.Trim();
        }

        return string.IsNullOrWhiteSpace(item.NameEn)
            ? report.Texts.ProductFallback
            : item.NameEn.Trim();
    }

    private static string FormatUnit(string? unit, DiaryReportData? report) {
        if (string.IsNullOrWhiteSpace(unit)) {
            return report?.Texts.GramsUnit ?? "g";
        }

        var normalized = unit.Trim();
        return normalized.Equals("g", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("gram", StringComparison.OrdinalIgnoreCase) ||
               normalized.Equals("grams", StringComparison.OrdinalIgnoreCase)
            ? report?.Texts.GramsUnit ?? "g"
            : normalized;
    }

    private static string CapitalizeFirstLetter(string value, CultureInfo culture) {
        if (string.IsNullOrWhiteSpace(value)) {
            return value;
        }

        var normalized = value.Trim();
        var firstTextElement = StringInfo.GetNextTextElement(normalized, 0);
        return firstTextElement.ToUpper(culture) + normalized[firstTextElement.Length..];
    }

    private static MealCompositionNutrition CalculateMealItemNutrition(MealItem item) {
        if (item.Product is not null) {
            var baseAmount = item.Product.BaseAmount <= 0 ? 1 : item.Product.BaseAmount;
            var multiplier = item.Amount / baseAmount;
            return new MealCompositionNutrition(
                item.Product.CaloriesPerBase * multiplier,
                item.Product.ProteinsPerBase * multiplier,
                item.Product.FatsPerBase * multiplier,
                item.Product.CarbsPerBase * multiplier,
                item.Product.FiberPerBase * multiplier);
        }

        if (item.Recipe is not null) {
            var servings = item.Recipe.Servings <= 0 ? 1 : item.Recipe.Servings;
            var multiplier = item.Amount / servings;
            return new MealCompositionNutrition(
                (item.Recipe.TotalCalories ?? 0) * multiplier,
                (item.Recipe.TotalProteins ?? 0) * multiplier,
                (item.Recipe.TotalFats ?? 0) * multiplier,
                (item.Recipe.TotalCarbs ?? 0) * multiplier,
                (item.Recipe.TotalFiber ?? 0) * multiplier);
        }

        return new MealCompositionNutrition(0, 0, 0, 0, 0);
    }

    private sealed record MealCompositionItem(
        string Label,
        string Name,
        string Amount,
        double Calories,
        double Proteins,
        double Fats,
        double Carbs,
        double Fiber);

    private readonly record struct MealCompositionNutrition(
        double Calories,
        double Proteins,
        double Fats,
        double Carbs,
        double Fiber);
}
