using System.Globalization;
using FoodDiary.Domain.Entities.Meals;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private static void ComposeMealsCards(IContainer container, DiaryReportData report) {
        IReadOnlyList<Meal> meals = report.Meals;

        container.Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(12).Column(column => {
            column.Spacing(10);
            column.Item().Text(report.Texts.MealsTitle).FontSize(12).SemiBold().FontColor(MutedTextColor);

            if (meals.Count == 0) {
                column.Item().PaddingVertical(20).AlignCenter()
                    .Text(report.Texts.NoMealsMessage)
                    .FontSize(12).FontColor(MutedTextColor);
                return;
            }

            foreach (Meal meal in meals) {
                column.Item().ShowEntire().Element(c => ComposeMealCard(c, report, meal));
            }
        });
    }

    private static void ComposeMealCard(IContainer container, DiaryReportData report, Meal meal) {
        bool hasImage = report.MealImages.TryGetValue(meal.Id, out byte[]? imageBytes);

        container.Background(CardBackground).Border(1).BorderColor(BorderColor).Padding(8).Row(row => {
            row.Spacing(10);

            if (hasImage && imageBytes is not null) {
                row.ConstantItem(74).Height(74).Element(c => ComposeMealImage(c, imageBytes));
            } else {
                row.ConstantItem(74).Height(74).Element(ComposeMealImagePlaceholder);
            }

            row.RelativeItem().Column(column => {
                column.Spacing(6);

                column.Item().Row(header => {
                    header.RelativeItem().Text(text => {
                        text.Span(report.FormatMealDate(meal.Date)).FontSize(11).Bold().FontColor(TextColor);
                        text.Span("  ");
                        text.Span(report.FormatMealType(meal.MealType)).FontSize(9).SemiBold().FontColor(MutedTextColor);
                    });

                    header.ConstantItem(88).AlignRight().Text($"{FormatNumber(EffectiveCalories(meal), 0, report.Culture)} {report.Texts.KcalUnit}")
                        .FontSize(16).Bold().FontColor(TextColor);
                });

                column.Item().Row(rowMetrics => {
                    rowMetrics.Spacing(6);
                    rowMetrics.RelativeItem().Element(c => ComposeMetricPill(c, report, report.Texts.ProteinsTitle, EffectiveProteins(meal), report.Texts.GramsUnit, ProteinColor));
                    rowMetrics.RelativeItem().Element(c => ComposeMetricPill(c, report, report.Texts.FatsTitle, EffectiveFats(meal), report.Texts.GramsUnit, FatColor));
                    rowMetrics.RelativeItem().Element(c => ComposeMetricPill(c, report, report.Texts.CarbsTitle, EffectiveCarbs(meal), report.Texts.GramsUnit, CarbColor));
                    rowMetrics.RelativeItem().Element(c => ComposeMetricPill(c, report, report.Texts.FiberTitle, EffectiveFiber(meal), report.Texts.GramsUnit, FiberColor));
                    rowMetrics.RelativeItem().Element(c => ComposeSatietyPill(c, report.Texts.BeforeLabel, meal.PreMealSatietyLevel));
                    rowMetrics.RelativeItem().Element(c => ComposeSatietyPill(c, report.Texts.AfterLabel, meal.PostMealSatietyLevel));
                });

                column.Item().Element(c => ComposeMealItemsList(c, report, meal));

                if (!string.IsNullOrWhiteSpace(meal.Comment)) {
                    column.Item().Column(comment => {
                        comment.Spacing(2);
                        comment.Item().Text($"{report.Texts.CommentColumn}:").FontSize(8).SemiBold().FontColor(MutedTextColor);
                        comment.Item().Text(Truncate(meal.Comment, 180)).FontSize(8).FontColor(TextColor);
                    });
                }
            });
        });
    }

    private static void ComposeMealsTable(IContainer container, DiaryReportData report) {
        IReadOnlyList<Meal> meals = report.Meals;

        container.Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(12).Column(column => {
            column.Spacing(10);
            column.Item().Text(report.Texts.MealsTitle).FontSize(12).SemiBold().FontColor(MutedTextColor);

            if (meals.Count == 0) {
                column.Item().PaddingVertical(20).AlignCenter()
                    .Text(report.Texts.NoMealsMessage)
                    .FontSize(12).FontColor(MutedTextColor);
                return;
            }

            column.Item().Table(table => {
                table.ColumnsDefinition(columns => {
                    columns.ConstantColumn(78);
                    columns.ConstantColumn(48);
                    columns.RelativeColumn(2.2f);
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(42);
                    columns.ConstantColumn(58);
                    columns.RelativeColumn();
                });

                table.Header(header => {
                    foreach (string headerText in GetMealTableHeaders(report)) {
                        HeaderCell(header.Cell(), headerText);
                    }
                });

                foreach (Meal meal in meals) {
                    foreach (string value in GetMealTableValues(meal, report)) {
                        DataCell(table.Cell(), value);
                    }
                }
            });
        });
    }

    private static string[] GetMealTableHeaders(DiaryReportData report) => [
        report.Texts.DateColumn,
        report.Texts.TypeColumn,
        report.Texts.ItemsColumn,
        report.Texts.KcalColumn,
        report.Texts.ProteinsColumnShort,
        report.Texts.FatsTitle,
        report.Texts.CarbsTitle,
        report.Texts.FiberTitle,
        report.Texts.SatietyColumn,
        report.Texts.CommentColumn,
    ];

    private static string[] GetMealTableValues(Meal meal, DiaryReportData report) => [
        report.FormatMealDate(meal.Date),
        report.FormatMealType(meal.MealType),
        FormatMealItemsList(meal, report),
        FormatNumber(EffectiveCalories(meal), 0, report.Culture),
        FormatNumber(EffectiveProteins(meal), 1, report.Culture),
        FormatNumber(EffectiveFats(meal), 1, report.Culture),
        FormatNumber(EffectiveCarbs(meal), 1, report.Culture),
        FormatNumber(EffectiveFiber(meal), 1, report.Culture),
        string.Create(CultureInfo.InvariantCulture, $"{meal.PreMealSatietyLevel}/{meal.PostMealSatietyLevel}"),
        string.IsNullOrWhiteSpace(meal.Comment) ? "" : Truncate(meal.Comment, 90),
    ];

    private static void ComposeMealItemsList(IContainer container, DiaryReportData report, Meal meal) {
        IReadOnlyList<MealCompositionItem> compositionItems = GetMealCompositionItems(meal, report);
        container.Column(column => {
            column.Spacing(3);

            if (compositionItems.Count == 0) {
                column.Item().Text(report.Texts.ItemsNotSpecified).FontSize(8).FontColor(MutedTextColor);
                return;
            }

            column.Item()
                .BorderTop(0.75f)
                .BorderLeft(0.75f)
                .BorderRight(0.75f)
                .BorderColor(BorderColor)
                .Table(table => {
                    table.ColumnsDefinition(columns => {
                        columns.RelativeColumn(1.7f);
                        columns.ConstantColumn(44);
                        columns.ConstantColumn(34);
                        columns.ConstantColumn(42);
                        columns.ConstantColumn(42);
                        columns.ConstantColumn(42);
                        columns.ConstantColumn(42);
                    });

                    CompositionHeaderCell(table.Cell(), report.Texts.ItemsColumn);
                    CompositionHeaderCell(table.Cell(), report.Texts.AmountColumn);
                    CompositionHeaderCell(table.Cell(), report.Texts.KcalColumn);
                    CompositionHeaderCell(table.Cell(), report.Texts.ProteinsColumnShort);
                    CompositionHeaderCell(table.Cell(), report.Texts.FatsColumnShort);
                    CompositionHeaderCell(table.Cell(), report.Texts.CarbsColumnShort);
                    CompositionHeaderCell(table.Cell(), report.Texts.FiberColumnShort);

                    foreach (MealCompositionItem item in compositionItems) {
                        DataCell(table.Cell(), Truncate(item.Name, 44));
                        DataCell(table.Cell(), item.Amount);
                        DataCell(table.Cell(), FormatNumber(item.Calories, 0, report.Culture));
                        DataCell(table.Cell(), FormatNumber(item.Proteins, 1, report.Culture));
                        DataCell(table.Cell(), FormatNumber(item.Fats, 1, report.Culture));
                        DataCell(table.Cell(), FormatNumber(item.Carbs, 1, report.Culture));
                        DataCell(table.Cell(), FormatNumber(item.Fiber, 1, report.Culture));
                    }
                });
        });
    }

    private static void HeaderCell(IContainer container, string value) {
        container.Background(TableHeaderBackground).PaddingHorizontal(5).PaddingVertical(6)
            .Text(value).FontSize(7).SemiBold().FontColor(MutedTextColor);
    }

    private static void CompositionHeaderCell(IContainer container, string value) {
        container.Background(TableHeaderBackground).BorderBottom(0.75f).BorderColor(BorderColor).PaddingHorizontal(5).PaddingVertical(6)
            .Text(value).FontSize(7).SemiBold().FontColor(MutedTextColor);
    }

    private static void DataCell(IContainer container, string value) {
        container.BorderBottom(0.5f).BorderColor(BorderColor).PaddingHorizontal(5).PaddingVertical(5)
            .Text(value).FontSize(6.5f).FontColor(TextColor);
    }

    private static void ComposeMealImage(IContainer container, byte[] imageBytes) {
        container.Background(ImagePlaceholderBackground).Border(1).BorderColor(BorderColor)
            .AlignCenter()
            .AlignMiddle()
            .Image(imageBytes)
            .FitArea();
    }

    private static void ComposeMealImagePlaceholder(IContainer container) {
        container.Background(ImagePlaceholderBackground).Border(1).BorderColor(BorderColor)
            .AlignCenter()
            .AlignMiddle()
            .Text("?")
            .FontSize(36)
            .Bold()
            .FontColor(MutedTextColor);
    }

    private static void ComposeMetricPill(IContainer container, DiaryReportData report, string label, double value, string unit, string color) {
        container.Background(MetricBackground).Border(1).BorderColor(BorderColor).PaddingHorizontal(5).PaddingVertical(4).Column(column => {
            column.Item().Text(label).FontSize(6).FontColor(MutedTextColor);
            column.Item().Text(text => {
                text.Span(FormatNumber(value, 1, report.Culture)).FontSize(9).Bold().FontColor(color);
                text.Span($" {unit}").FontSize(7).FontColor(MutedTextColor);
            });
        });
    }

    private static void ComposeSatietyPill(IContainer container, string label, int level) {
        container.Background(MetricBackground).Border(1).BorderColor(BorderColor).PaddingHorizontal(5).PaddingVertical(4).Column(column => {
            column.Item().Text(label).FontSize(6).FontColor(MutedTextColor);
            column.Item().Row(row => {
                row.Spacing(2);
                for (int index = 1; index <= 5; index++) {
                    string color = index <= level ? SatietyColor : BorderColor;
                    row.RelativeItem().Height(4).Background(color);
                }
            });
            column.Item().Text(string.Create(CultureInfo.InvariantCulture, $"{level}/5")).FontSize(7).FontColor(TextColor);
        });
    }
}
