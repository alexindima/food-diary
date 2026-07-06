using System.Globalization;
using System.Runtime.InteropServices;
using FoodDiary.Application.Abstractions.Meals.Models;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private static string FormatMealItems(MealConsumptionReadModel meal, DiaryReportData report) {
        string items = FormatMealItemsList(meal, report);
        return string.Equals(items, report.Texts.ItemsNotSpecified, StringComparison.Ordinal)
            ? $"{report.Texts.ItemsPrefix}: {report.Texts.ItemsNotSpecified}"
            : $"{report.Texts.ItemsPrefix}: {items}";
    }

    private static string FormatMealItemsList(MealConsumptionReadModel meal, DiaryReportData report) {
        IReadOnlyList<string> itemLabels = FormatMealItemLabels(meal, report, maxItems: 6);
        return itemLabels.Count == 0
            ? report.Texts.ItemsNotSpecified
            : Truncate(string.Join(", ", itemLabels), 220);
    }

    private static IReadOnlyList<string> FormatMealItemLabels(MealConsumptionReadModel meal, DiaryReportData report, int maxItems) {
        IReadOnlyList<MealCompositionItem> compositionItems = GetMealCompositionItems(meal, report);
        string[] itemLabels = [.. compositionItems
            .Select(item => item.Label)
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Take(maxItems)];

        if (itemLabels.Length == 0) {
            return [];
        }

        string suffix = compositionItems.Count > itemLabels.Length
            ? string.Create(CultureInfo.InvariantCulture, $" +{compositionItems.Count - itemLabels.Length} {report.Texts.MoreItemsSuffix}")
            : "";

        return suffix.Length == 0
            ? itemLabels
            : [.. itemLabels, suffix.TrimStart()];
    }

    private static IReadOnlyList<MealCompositionItem> GetMealCompositionItems(MealConsumptionReadModel meal, DiaryReportData report) {
        MealCompositionItem[] manualItems = [.. meal.Items
            .OrderBy(item => item.Id)
            .Select(item => FormatMealItem(item, report))];

        MealCompositionItem[] aiItems = [.. meal.AiSessions
            .OrderBy(session => session.RecognizedAtUtc)
            .SelectMany(session => session.Items.OrderBy(item => item.Id))
            .Select(item => FormatMealAiItem(item, report))];

        return [.. manualItems, .. aiItems];
    }

    private static MealCompositionItem FormatMealItem(MealConsumptionItemReadModel item, DiaryReportData report) {
        bool isRecipe = item.RecipeId.HasValue;
        string? name = item.ProductName ?? item.RecipeName;
        if (string.IsNullOrWhiteSpace(name)) {
            name = isRecipe ? report.Texts.RecipeFallback : report.Texts.ProductFallback;
        }

        string amountUnit = isRecipe ? report.Texts.ServingUnit : FormatProductUnit(item, report);
        string amount = $"{FormatNumber(item.Amount, isRecipe ? 1 : 0, report.Culture)} {amountUnit}";
        MealCompositionNutrition nutrition = CalculateMealItemNutrition(item);

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

    private static string FormatProductUnit(MealConsumptionItemReadModel item, DiaryReportData report) =>
        FormatUnit(item.ProductBaseUnit, report);

    private static MealCompositionItem FormatMealAiItem(MealConsumptionAiItemReadModel item, DiaryReportData report) {
        string name = ResolveAiItemName(item, report);
        string unit = FormatUnit(item.Unit, report);
        string amount = $"{FormatNumber(item.Amount, 0, report.Culture)} {unit}";

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

    private static string ResolveAiItemName(MealConsumptionAiItemReadModel item, DiaryReportData report) {
        if (!string.Equals(report.Culture.TwoLetterISOLanguageName, "en", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(item.NameLocal)) {
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

        string normalized = unit.Trim();
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

        string normalized = value.Trim();
        string firstTextElement = StringInfo.GetNextTextElement(normalized, 0);
        return firstTextElement.ToUpper(culture) + normalized[firstTextElement.Length..];
    }

    private static MealCompositionNutrition CalculateMealItemNutrition(MealConsumptionItemReadModel item) {
        if (item.ProductId.HasValue) {
            double baseAmount = item.ProductBaseAmount is > 0 ? item.ProductBaseAmount.Value : 1;
            double multiplier = item.Amount / baseAmount;
            return new MealCompositionNutrition(
                (item.ProductCaloriesPerBase ?? 0) * multiplier,
                (item.ProductProteinsPerBase ?? 0) * multiplier,
                (item.ProductFatsPerBase ?? 0) * multiplier,
                (item.ProductCarbsPerBase ?? 0) * multiplier,
                (item.ProductFiberPerBase ?? 0) * multiplier);
        }

        if (item.RecipeId.HasValue) {
            int servings = item.RecipeServings is > 0 ? item.RecipeServings.Value : 1;
            double multiplier = item.Amount / servings;
            return new MealCompositionNutrition(
                (item.RecipeTotalCalories ?? 0) * multiplier,
                (item.RecipeTotalProteins ?? 0) * multiplier,
                (item.RecipeTotalFats ?? 0) * multiplier,
                (item.RecipeTotalCarbs ?? 0) * multiplier,
                (item.RecipeTotalFiber ?? 0) * multiplier);
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

    [StructLayout(LayoutKind.Auto)]
    private readonly record struct MealCompositionNutrition(
        double Calories,
        double Proteins,
        double Fats,
        double Carbs,
        double Fiber);
}
