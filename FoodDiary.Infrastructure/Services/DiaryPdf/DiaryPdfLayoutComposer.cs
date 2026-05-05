using System.Globalization;
using FoodDiary.Domain.Entities.Meals;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private static void ComposeHeader(IContainer container, DiaryReportData report) {
        container.PaddingBottom(12).Row(row => {
            row.RelativeItem().Column(column => {
                column.Item().Text(report.Texts.ReportTitle)
                    .FontSize(18).Bold().FontColor(TextColor);

                column.Item().Text(text => {
                    text.Span($"{report.Texts.PeriodLabel}: ").SemiBold().FontColor(MutedTextColor);
                    text.Span($"{report.PeriodStartLabel} - {report.PeriodEndLabel}");
                    text.Span($" ({report.TimeZoneOffsetLabel})").FontColor(MutedTextColor);
                    text.Span("  |  ").FontColor(BorderColor);
                    text.Span(string.Format(report.Culture, report.Texts.MealsCountLabel, report.MealCount)).FontColor(MutedTextColor);
                });
            });

            row.ConstantItem(150).AlignRight().Text(DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture))
                .FontSize(8).FontColor(MutedTextColor);
        });
    }

    private static void ComposeContent(IContainer container, DiaryReportData report) {
        container.Column(column => {
            column.Spacing(10);
            column.Item().Element(c => ComposeSummarySection(c, report));
            column.Item().PageBreak();
            column.Item().Element(c => ComposeNutritionChartSection(c, report));
            column.Item().PageBreak();
            column.Item().Element(c => {
                if (report.UseCompactMealsMode) {
                    ComposeMealsTable(c, report);
                } else {
                    ComposeMealsCards(c, report);
                }
            });
        });
    }

    private static void ComposeSummarySection(IContainer container, DiaryReportData report) {
        container.Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(12).Column(column => {
            column.Spacing(12);
            column.Item().Text(report.Texts.PeriodSummaryTitle).FontSize(13).SemiBold().FontColor(MutedTextColor);

            column.Item().Row(row => {
                row.Spacing(8);

                row.RelativeItem(2).Element(c => ComposeTotalCaloriesCard(c, report));
                row.RelativeItem().Element(c => ComposeAverageCard(c, report));
            });

            column.Item().Row(row => {
                row.Spacing(8);

                row.RelativeItem().Element(c => ComposeMacroCard(c, report, report.Texts.ProteinsTitle, report.AverageProteins, ProteinColor, report.ProteinSeries));
                row.RelativeItem().Element(c => ComposeMacroCard(c, report, report.Texts.FatsTitle, report.AverageFats, FatColor, report.FatSeries));
                row.RelativeItem().Element(c => ComposeMacroCard(c, report, report.Texts.CarbsTitle, report.AverageCarbs, CarbColor, report.CarbSeries));
                row.RelativeItem().Element(c => ComposeMacroCard(c, report, report.Texts.FiberTitle, report.AverageFiber, FiberColor, report.FiberSeries));
            });
        });
    }

    private static void ComposeTotalCaloriesCard(IContainer container, DiaryReportData report) {
        container.Background(CardBackground).Border(1).BorderColor(BorderColor).Padding(12).Height(180).Column(column => {
            column.Item().Text(report.Texts.TotalCaloriesTitle).FontSize(9).SemiBold().FontColor(MutedTextColor);
            column.Item().Text($"{FormatNumber(report.TotalCalories, 0, report.Culture)} {report.Texts.KcalUnit}").FontSize(26).Bold().FontColor(TextColor);
            column.Item().ExtendVertical().AlignBottom().Svg(DiaryChartSvgRenderer.RenderWideSparkline(report.CalorieSeries, PrimaryColor, PrimaryFillColor)).FitArea();
        });
    }

    private static void ComposeAverageCard(IContainer container, DiaryReportData report) {
        container.Background(CardBackground).Border(1).BorderColor(BorderColor).Padding(12).Height(180).Column(column => {
            column.Item().Text(report.Texts.AveragePerDayTitle).FontSize(9).SemiBold().FontColor(MutedTextColor);
            column.Item().Text($"{FormatNumber(report.AverageCalories, 0, report.Culture)} {report.Texts.KcalUnit}").FontSize(26).Bold().FontColor(TextColor);
            column.Item().PaddingTop(14).LineHorizontal(1).LineColor(BorderColor);
            column.Item().PaddingTop(10).Text(report.Texts.TotalForPeriodTitle).FontSize(8).SemiBold().FontColor(MutedTextColor);
            column.Item().PaddingTop(8).Column(metrics => {
                metrics.Spacing(8);
                metrics.Item().Row(row => {
                    row.RelativeItem().Column(metric => {
                        metric.Item().Text(FormatNumber(report.TotalProteins, 1, report.Culture)).FontSize(16).Bold();
                        metric.Item().Text(report.Texts.GramsProteinsLabel).FontSize(7).FontColor(MutedTextColor);
                    });
                    row.RelativeItem().Column(metric => {
                        metric.Item().Text(FormatNumber(report.TotalFats, 1, report.Culture)).FontSize(16).Bold();
                        metric.Item().Text(report.Texts.GramsFatsLabel).FontSize(7).FontColor(MutedTextColor);
                    });
                });
                metrics.Item().Row(row => {
                    row.RelativeItem().Column(metric => {
                        metric.Item().Text(FormatNumber(report.TotalCarbs, 1, report.Culture)).FontSize(16).Bold();
                        metric.Item().Text(report.Texts.GramsCarbsLabel).FontSize(7).FontColor(MutedTextColor);
                    });
                    row.RelativeItem().Column(metric => {
                        metric.Item().Text(FormatNumber(report.TotalFiber, 1, report.Culture)).FontSize(16).Bold();
                        metric.Item().Text(report.Texts.GramsFiberLabel).FontSize(7).FontColor(MutedTextColor);
                    });
                });
            });
        });
    }

    private static void ComposeMacroCard(IContainer container, DiaryReportData report, string title, double value, string color, IReadOnlyList<double> series) {
        container.Background(CardBackground).BorderTop(3).BorderColor(color).Padding(8).Height(92).Column(column => {
            column.Item().Text(title).FontSize(8).SemiBold().FontColor(MutedTextColor);
            column.Item().Text($"{FormatNumber(value, 1, report.Culture)} {report.Texts.GramsUnit}").FontSize(20).Bold().FontColor(TextColor);
            column.Item().ExtendVertical().AlignBottom().Svg(DiaryChartSvgRenderer.RenderSparkline(series, color, ApplyAlpha(color, 0.12))).FitArea();
        });
    }

    private static void ComposeNutritionChartSection(IContainer container, DiaryReportData report) {
        container.Column(column => {
            column.Spacing(12);
            column.Item().Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(10).Column(chart => {
                chart.Spacing(8);
                chart.Item().Text(report.Texts.CaloriesByDayTitle).FontSize(12).SemiBold().FontColor(MutedTextColor);
                chart.Item().Height(150).Svg(DiaryChartSvgRenderer.RenderLineChart(
                    report.DayLabels,
                    report.CalorieSeries,
                    PrimaryColor,
                    PrimaryFillColor)).FitArea();
            });
            column.Item().Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(10).Column(chart => {
                chart.Spacing(8);
                chart.Item().Text(report.Texts.NutrientsByDayTitle).FontSize(12).SemiBold().FontColor(MutedTextColor);
                chart.Item().Height(150).Svg(DiaryChartSvgRenderer.RenderMultiLineChart(
                    report.DayLabels,
                    [
                        new ChartSeries(report.Texts.ProteinsTitle, report.ProteinSeries, ProteinColor),
                        new ChartSeries(report.Texts.FatsTitle, report.FatSeries, FatColor),
                        new ChartSeries(report.Texts.CarbsTitle, report.CarbSeries, CarbColor),
                        new ChartSeries(report.Texts.FiberTitle, report.FiberSeries, FiberColor),
                    ])).FitArea();
            });
        });
    }

    private static void ComposeMealsCards(IContainer container, DiaryReportData report) {
        var meals = report.Meals;

        container.Background(PanelBackground).Border(1).BorderColor(BorderColor).Padding(12).Column(column => {
            column.Spacing(10);
            column.Item().Text(report.Texts.MealsTitle).FontSize(12).SemiBold().FontColor(MutedTextColor);

            if (meals.Count == 0) {
                column.Item().PaddingVertical(20).AlignCenter()
                    .Text(report.Texts.NoMealsMessage)
                    .FontSize(12).FontColor(MutedTextColor);
                return;
            }

            foreach (var meal in meals) {
                column.Item().ShowEntire().Element(c => ComposeMealCard(c, report, meal));
            }
        });
    }

    private static void ComposeMealCard(IContainer container, DiaryReportData report, Meal meal) {
        var hasImage = report.MealImages.TryGetValue(meal.Id, out var imageBytes);

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

                column.Item().Text(FormatMealItems(meal, report)).FontSize(8).FontColor(MutedTextColor);

                column.Item().Row(rowMetrics => {
                    rowMetrics.Spacing(6);
                    rowMetrics.RelativeItem().Element(c => ComposeMetricPill(c, report, report.Texts.ProteinsTitle, EffectiveProteins(meal), report.Texts.GramsUnit, ProteinColor));
                    rowMetrics.RelativeItem().Element(c => ComposeMetricPill(c, report, report.Texts.FatsTitle, EffectiveFats(meal), report.Texts.GramsUnit, FatColor));
                    rowMetrics.RelativeItem().Element(c => ComposeMetricPill(c, report, report.Texts.CarbsTitle, EffectiveCarbs(meal), report.Texts.GramsUnit, CarbColor));
                    rowMetrics.RelativeItem().Element(c => ComposeMetricPill(c, report, report.Texts.FiberTitle, EffectiveFiber(meal), report.Texts.GramsUnit, FiberColor));
                    rowMetrics.RelativeItem().Element(c => ComposeSatietyPill(c, report.Texts.BeforeLabel, meal.PreMealSatietyLevel));
                    rowMetrics.RelativeItem().Element(c => ComposeSatietyPill(c, report.Texts.AfterLabel, meal.PostMealSatietyLevel));
                });

                if (!string.IsNullOrWhiteSpace(meal.Comment)) {
                    column.Item().Text(Truncate(meal.Comment, 180)).FontSize(8).FontColor(MutedTextColor);
                }
            });
        });
    }

    private static void ComposeMealsTable(IContainer container, DiaryReportData report) {
        var meals = report.Meals;

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
                    HeaderCell(header.Cell(), report.Texts.DateColumn);
                    HeaderCell(header.Cell(), report.Texts.TypeColumn);
                    HeaderCell(header.Cell(), report.Texts.ItemsColumn);
                    HeaderCell(header.Cell(), report.Texts.KcalColumn);
                    HeaderCell(header.Cell(), report.Texts.ProteinsColumnShort);
                    HeaderCell(header.Cell(), report.Texts.FatsTitle);
                    HeaderCell(header.Cell(), report.Texts.CarbsTitle);
                    HeaderCell(header.Cell(), report.Texts.FiberTitle);
                    HeaderCell(header.Cell(), report.Texts.SatietyColumn);
                    HeaderCell(header.Cell(), report.Texts.CommentColumn);
                });

                foreach (var meal in meals) {
                    DataCell(table.Cell(), report.FormatMealDate(meal.Date));
                    DataCell(table.Cell(), report.FormatMealType(meal.MealType));
                    DataCell(table.Cell(), FormatMealItemsList(meal, report));
                    DataCell(table.Cell(), FormatNumber(EffectiveCalories(meal), 0, report.Culture));
                    DataCell(table.Cell(), FormatNumber(EffectiveProteins(meal), 1, report.Culture));
                    DataCell(table.Cell(), FormatNumber(EffectiveFats(meal), 1, report.Culture));
                    DataCell(table.Cell(), FormatNumber(EffectiveCarbs(meal), 1, report.Culture));
                    DataCell(table.Cell(), FormatNumber(EffectiveFiber(meal), 1, report.Culture));
                    DataCell(table.Cell(), $"{meal.PreMealSatietyLevel}/{meal.PostMealSatietyLevel}");
                    DataCell(table.Cell(), string.IsNullOrWhiteSpace(meal.Comment) ? "" : Truncate(meal.Comment, 90));
                }
            });
        });
    }

    private static void HeaderCell(IContainer container, string value) {
        container.Background(TableHeaderBackground).PaddingHorizontal(5).PaddingVertical(6)
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
                for (var index = 1; index <= 5; index++) {
                    var color = index <= level ? SatietyColor : BorderColor;
                    row.RelativeItem().Height(4).Background(color);
                }
            });
            column.Item().Text($"{level}/5").FontSize(7).FontColor(TextColor);
        });
    }

    private static void ComposeFooter(IContainer container, DiaryReportData report) {
        container.AlignCenter().Text(text => {
            text.Span(report.Texts.GeneratedByPrefix).FontSize(7).FontColor(MutedTextColor);
            text.Span(report.ReportHost).FontSize(7).FontColor(PrimaryColor);
        });
    }
}
