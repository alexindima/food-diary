using System.Globalization;
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

            row.ConstantItem(150).AlignRight().Text(report.GeneratedAtUtc.ToString("yyyy-MM-dd HH:mm 'UTC'", CultureInfo.InvariantCulture))
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

    private static void ComposeFooter(IContainer container, DiaryReportData report) {
        container.AlignCenter().Text(text => {
            text.Span(report.Texts.GeneratedByPrefix).FontSize(7).FontColor(MutedTextColor);
            text.Span(report.ReportHost).FontSize(7).FontColor(PrimaryColor);
        });
    }
}
