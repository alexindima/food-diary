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
        Math.Round(value, decimals, MidpointRounding.ToEven).ToString(string.Create(CultureInfo.InvariantCulture, $"N{decimals}"), culture);

    private static string ApplyAlpha(string hex, double alpha) {
        string normalized = hex.TrimStart('#');
        if (normalized.Length != 6) {
            return hex;
        }

        byte alphaByte = (byte)Math.Round(Math.Clamp(alpha, 0, 1) * 255, MidpointRounding.ToEven);
        return $"#{alphaByte:X2}{normalized}";
    }

    private static string ResolveReportHost(string? reportOrigin) {
        if (string.IsNullOrWhiteSpace(reportOrigin)) {
            return DefaultReportHost;
        }

        string normalized = reportOrigin.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out Uri? uri) &&
            uri.Scheme is "http" or "https" &&
            !string.IsNullOrWhiteSpace(uri.Host)) {
            return FormatHost(uri.IdnHost, uri.Port, uri.IsDefaultPort);
        }

        return Uri.CheckHostName(normalized) == UriHostNameType.Unknown
            ? DefaultReportHost
            : FormatHost(normalized, port: null, isDefaultPort: true);
    }

    private static string FormatHost(string host, int? port, bool isDefaultPort) {
        string unicodeHost = ToUnicodeHost(host.Trim().TrimEnd('.'));
        return port.HasValue && !isDefaultPort
            ? string.Create(CultureInfo.InvariantCulture, $"{unicodeHost}:{port.Value}"
)
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
}
