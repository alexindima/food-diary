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
        if (meal.Items.Count == 0) {
            return report.Texts.ItemsNotSpecified;
        }

        var itemLabels = meal.Items
            .OrderBy(item => item.CreatedOnUtc)
            .Select(item => FormatMealItem(item, report))
            .Where(label => !string.IsNullOrWhiteSpace(label))
            .Take(6)
            .ToArray();

        if (itemLabels.Length == 0) {
            return report.Texts.ItemsNotSpecified;
        }

        var suffix = meal.Items.Count > itemLabels.Length
            ? $" +{meal.Items.Count - itemLabels.Length} {report.Texts.MoreItemsSuffix}"
            : "";

        return Truncate($"{string.Join(", ", itemLabels)}{suffix}", 220);
    }

    private static string FormatMealItem(MealItem item, DiaryReportData report) {
        var name = item.Product?.Name ?? item.Recipe?.Name;
        if (string.IsNullOrWhiteSpace(name)) {
            name = item.IsRecipe ? report.Texts.RecipeFallback : report.Texts.ProductFallback;
        }

        var amountUnit = item.IsRecipe ? report.Texts.ServingUnit : FormatProductUnit(item);
        return $"{FormatNumber(item.Amount, item.IsRecipe ? 1 : 0, report.Culture)} {amountUnit} {name}";
    }

    private static string FormatProductUnit(MealItem item) =>
        item.Product?.BaseUnit.ToString().ToLowerInvariant() ?? "g";
}
