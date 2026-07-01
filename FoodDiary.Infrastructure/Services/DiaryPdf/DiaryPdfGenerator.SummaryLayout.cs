using QuestPDF.Fluent;
using QuestPDF.Infrastructure;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
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
}
