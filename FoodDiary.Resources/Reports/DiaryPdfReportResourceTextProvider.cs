using System.Globalization;
using System.Resources;
using FoodDiary.Application.Abstractions.Export.Common;

namespace FoodDiary.Resources.Reports;

public sealed class DiaryPdfReportResourceTextProvider : IDiaryPdfReportTextProvider {
    private static readonly ResourceManager ResourceManager =
        new("FoodDiary.Resources.Reports.DiaryPdfReport", typeof(DiaryPdfReportResourceTextProvider).Assembly);

    public DiaryPdfReportTexts GetTexts(string? locale) {
        var culture = ResolveCulture(locale);
        return new DiaryPdfReportTexts(
            CultureName: culture.Name,
            ReportTitle: GetRequired(nameof(DiaryPdfReportTexts.ReportTitle), culture),
            PeriodLabel: GetRequired(nameof(DiaryPdfReportTexts.PeriodLabel), culture),
            MealsCountLabel: GetRequired(nameof(DiaryPdfReportTexts.MealsCountLabel), culture),
            PeriodSummaryTitle: GetRequired(nameof(DiaryPdfReportTexts.PeriodSummaryTitle), culture),
            TotalCaloriesTitle: GetRequired(nameof(DiaryPdfReportTexts.TotalCaloriesTitle), culture),
            KcalUnit: GetRequired(nameof(DiaryPdfReportTexts.KcalUnit), culture),
            AveragePerDayTitle: GetRequired(nameof(DiaryPdfReportTexts.AveragePerDayTitle), culture),
            TotalForPeriodTitle: GetRequired(nameof(DiaryPdfReportTexts.TotalForPeriodTitle), culture),
            ProteinsTitle: GetRequired(nameof(DiaryPdfReportTexts.ProteinsTitle), culture),
            FatsTitle: GetRequired(nameof(DiaryPdfReportTexts.FatsTitle), culture),
            CarbsTitle: GetRequired(nameof(DiaryPdfReportTexts.CarbsTitle), culture),
            FiberTitle: GetRequired(nameof(DiaryPdfReportTexts.FiberTitle), culture),
            GramsUnit: GetRequired(nameof(DiaryPdfReportTexts.GramsUnit), culture),
            GramsProteinsLabel: GetRequired(nameof(DiaryPdfReportTexts.GramsProteinsLabel), culture),
            GramsFatsLabel: GetRequired(nameof(DiaryPdfReportTexts.GramsFatsLabel), culture),
            GramsCarbsLabel: GetRequired(nameof(DiaryPdfReportTexts.GramsCarbsLabel), culture),
            GramsFiberLabel: GetRequired(nameof(DiaryPdfReportTexts.GramsFiberLabel), culture),
            CaloriesByDayTitle: GetRequired(nameof(DiaryPdfReportTexts.CaloriesByDayTitle), culture),
            NutrientsByDayTitle: GetRequired(nameof(DiaryPdfReportTexts.NutrientsByDayTitle), culture),
            MealsTitle: GetRequired(nameof(DiaryPdfReportTexts.MealsTitle), culture),
            NoMealsMessage: GetRequired(nameof(DiaryPdfReportTexts.NoMealsMessage), culture),
            DateColumn: GetRequired(nameof(DiaryPdfReportTexts.DateColumn), culture),
            TypeColumn: GetRequired(nameof(DiaryPdfReportTexts.TypeColumn), culture),
            ItemsColumn: GetRequired(nameof(DiaryPdfReportTexts.ItemsColumn), culture),
            AmountColumn: GetRequired(nameof(DiaryPdfReportTexts.AmountColumn), culture),
            KcalColumn: GetRequired(nameof(DiaryPdfReportTexts.KcalColumn), culture),
            ProteinsColumnShort: GetRequired(nameof(DiaryPdfReportTexts.ProteinsColumnShort), culture),
            FatsColumnShort: GetRequired(nameof(DiaryPdfReportTexts.FatsColumnShort), culture),
            CarbsColumnShort: GetRequired(nameof(DiaryPdfReportTexts.CarbsColumnShort), culture),
            FiberColumnShort: GetRequired(nameof(DiaryPdfReportTexts.FiberColumnShort), culture),
            SatietyColumn: GetRequired(nameof(DiaryPdfReportTexts.SatietyColumn), culture),
            CommentColumn: GetRequired(nameof(DiaryPdfReportTexts.CommentColumn), culture),
            BeforeLabel: GetRequired(nameof(DiaryPdfReportTexts.BeforeLabel), culture),
            AfterLabel: GetRequired(nameof(DiaryPdfReportTexts.AfterLabel), culture),
            OtherMealType: GetRequired(nameof(DiaryPdfReportTexts.OtherMealType), culture),
            BreakfastMealType: GetRequired(nameof(DiaryPdfReportTexts.BreakfastMealType), culture),
            LunchMealType: GetRequired(nameof(DiaryPdfReportTexts.LunchMealType), culture),
            DinnerMealType: GetRequired(nameof(DiaryPdfReportTexts.DinnerMealType), culture),
            SnackMealType: GetRequired(nameof(DiaryPdfReportTexts.SnackMealType), culture),
            ItemsPrefix: GetRequired(nameof(DiaryPdfReportTexts.ItemsPrefix), culture),
            ItemsNotSpecified: GetRequired(nameof(DiaryPdfReportTexts.ItemsNotSpecified), culture),
            MoreItemsSuffix: GetRequired(nameof(DiaryPdfReportTexts.MoreItemsSuffix), culture),
            RecipeFallback: GetRequired(nameof(DiaryPdfReportTexts.RecipeFallback), culture),
            ProductFallback: GetRequired(nameof(DiaryPdfReportTexts.ProductFallback), culture),
            ServingUnit: GetRequired(nameof(DiaryPdfReportTexts.ServingUnit), culture),
            GeneratedByPrefix: GetRequired(nameof(DiaryPdfReportTexts.GeneratedByPrefix), culture));
    }

    private static CultureInfo ResolveCulture(string? locale) {
        var normalized = string.IsNullOrWhiteSpace(locale)
            ? "en"
            : locale.Trim().ToLowerInvariant();

        var cultureName = normalized.StartsWith("ru", StringComparison.Ordinal)
            ? "ru"
            : "en";

        return CultureInfo.GetCultureInfo(cultureName);
    }

    private static string GetRequired(string key, CultureInfo culture) =>
        ResourceManager.GetString(key, culture)
        ?? ResourceManager.GetString(key, CultureInfo.GetCultureInfo("en"))
        ?? throw new InvalidOperationException($"Missing diary PDF report resource '{key}'.");
}
