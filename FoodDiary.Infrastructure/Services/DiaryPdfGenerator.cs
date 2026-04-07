using System.Globalization;
using FoodDiary.Application.Export.Common;
using FoodDiary.Domain.Entities.Meals;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoodDiary.Infrastructure.Services;

internal sealed class DiaryPdfGenerator : IDiaryPdfGenerator {
    public byte[] Generate(IReadOnlyList<Meal> meals, DateTime dateFrom, DateTime dateTo) {
        QuestPDF.Settings.License = LicenseType.Community;

        var document = Document.Create(container => {
            container.Page(page => {
                page.Size(PageSizes.A4.Landscape());
                page.MarginHorizontal(30);
                page.MarginVertical(20);
                page.DefaultTextStyle(x => x.FontSize(9));

                page.Header().Element(c => ComposeHeader(c, dateFrom, dateTo, meals.Count));
                page.Content().Element(c => ComposeContent(c, meals));
                page.Footer().Element(ComposeFooter);
            });
        });

        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, DateTime dateFrom, DateTime dateTo, int mealCount) {
        container.Column(column => {
            column.Item().Text("Food Diary Export")
                .FontSize(16).Bold().FontColor(Colors.Blue.Darken2);

            column.Item().Text(text => {
                text.Span("Period: ").SemiBold();
                text.Span($"{dateFrom:yyyy-MM-dd} — {dateTo:yyyy-MM-dd}");
                text.Span("  |  ").FontColor(Colors.Grey.Medium);
                text.Span($"{mealCount} meals");
            });

            column.Item().PaddingBottom(8).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
        });
    }

    private static void ComposeContent(IContainer container, IReadOnlyList<Meal> meals) {
        if (meals.Count == 0) {
            container.PaddingVertical(20).AlignCenter()
                .Text("No meals recorded in this period.")
                .FontSize(12).FontColor(Colors.Grey.Medium);
            return;
        }

        container.Table(table => {
            table.ColumnsDefinition(columns => {
                columns.RelativeColumn(2);   // Date
                columns.RelativeColumn(2);   // Meal Type
                columns.RelativeColumn(1.5f); // Calories
                columns.RelativeColumn(1.5f); // Proteins
                columns.RelativeColumn(1.5f); // Fats
                columns.RelativeColumn(1.5f); // Carbs
                columns.RelativeColumn(1.5f); // Fiber
                columns.RelativeColumn(3);   // Comment
            });

            // Header row
            HeaderCell(table, "Date");
            HeaderCell(table, "Meal Type");
            HeaderCell(table, "Calories");
            HeaderCell(table, "Proteins");
            HeaderCell(table, "Fats");
            HeaderCell(table, "Carbs");
            HeaderCell(table, "Fiber");
            HeaderCell(table, "Comment");

            var totalCalories = 0.0;
            var totalProteins = 0.0;
            var totalFats = 0.0;
            var totalCarbs = 0.0;
            var totalFiber = 0.0;

            for (var i = 0; i < meals.Count; i++) {
                var meal = meals[i];
                var cal = EffectiveValue(meal, meal.TotalCalories, meal.ManualCalories);
                var pro = EffectiveValue(meal, meal.TotalProteins, meal.ManualProteins);
                var fat = EffectiveValue(meal, meal.TotalFats, meal.ManualFats);
                var carb = EffectiveValue(meal, meal.TotalCarbs, meal.ManualCarbs);
                var fib = EffectiveValue(meal, meal.TotalFiber, meal.ManualFiber);

                totalCalories += cal;
                totalProteins += pro;
                totalFats += fat;
                totalCarbs += carb;
                totalFiber += fib;

                var bg = i % 2 == 0 ? Colors.White : Colors.Grey.Lighten4;

                DataCell(table, meal.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture), bg);
                DataCell(table, meal.MealType?.ToString() ?? "—", bg);
                DataCell(table, FormatNumber(cal), bg);
                DataCell(table, FormatNumber(pro), bg);
                DataCell(table, FormatNumber(fat), bg);
                DataCell(table, FormatNumber(carb), bg);
                DataCell(table, FormatNumber(fib), bg);
                DataCell(table, meal.Comment ?? "", bg);
            }

            // Totals row
            TotalCell(table, "Total");
            TotalCell(table, "");
            TotalCell(table, FormatNumber(totalCalories));
            TotalCell(table, FormatNumber(totalProteins));
            TotalCell(table, FormatNumber(totalFats));
            TotalCell(table, FormatNumber(totalCarbs));
            TotalCell(table, FormatNumber(totalFiber));
            TotalCell(table, "");
        });
    }

    private static void ComposeFooter(IContainer container) {
        container.AlignCenter().Text(text => {
            text.Span("Generated by Food Diary — ").FontSize(7).FontColor(Colors.Grey.Medium);
            text.Span("fooddiary.club").FontSize(7).FontColor(Colors.Blue.Medium);
        });
    }

    private static void HeaderCell(TableDescriptor table, string text) {
        table.Cell().Background(Colors.Blue.Darken2).Padding(5)
            .Text(text).FontColor(Colors.White).SemiBold().FontSize(9);
    }

    private static void DataCell(TableDescriptor table, string text, string bg) {
        table.Cell().Background(bg).BorderBottom(1).BorderColor(Colors.Grey.Lighten3)
            .Padding(4).Text(text).FontSize(8);
    }

    private static void TotalCell(TableDescriptor table, string text) {
        table.Cell().Background(Colors.Blue.Lighten5).BorderTop(2).BorderColor(Colors.Blue.Darken2)
            .Padding(5).Text(text).Bold().FontSize(9);
    }

    private static double EffectiveValue(Meal meal, double total, double? manual) =>
        meal.IsNutritionAutoCalculated ? total : manual ?? total;

    private static string FormatNumber(double value) =>
        Math.Round(value, 1).ToString("F1", CultureInfo.InvariantCulture);
}
