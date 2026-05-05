using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Domain.Entities.Meals;
using FoodDiary.Domain.ValueObjects.Ids;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator(
    HttpClient httpClient,
    IDiaryPdfReportTextProvider textProvider) : IDiaryPdfGenerator {
    internal DiaryPdfGenerator()
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(5) }, new DefaultDiaryPdfReportTextProvider()) {
    }

    internal DiaryPdfGenerator(HttpClient httpClient)
        : this(httpClient, new DefaultDiaryPdfReportTextProvider()) {
    }

    public async Task<byte[]> GenerateAsync(
        IReadOnlyList<Meal> meals,
        DateTime dateFrom,
        DateTime dateTo,
        string? locale,
        int? timeZoneOffsetMinutes,
        string? reportOrigin,
        CancellationToken cancellationToken) {
        QuestPDF.Settings.License = LicenseType.Community;

        var texts = textProvider.GetTexts(locale);
        var useCompactMealsMode = ShouldUseCompactMealsMode(dateFrom, dateTo);
        var mealImages = useCompactMealsMode
            ? new Dictionary<MealId, byte[]>()
            : await LoadMealImagesAsync(meals, cancellationToken);
        var report = DiaryReportData.Create(
            meals,
            dateFrom,
            dateTo,
            mealImages,
            useCompactMealsMode,
            texts,
            timeZoneOffsetMinutes,
            ResolveReportHost(reportOrigin));

        var document = Document.Create(container => {
            container.Page(page => {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(24);
                page.PageColor(PageBackground);
                page.DefaultTextStyle(x => x.FontSize(9).FontColor(TextColor));

                page.Header().Element(c => ComposeHeader(c, report));
                page.Content().Element(c => ComposeContent(c, report));
                page.Footer().Element(c => ComposeFooter(c, report));
            });
        });

        return document.GeneratePdf();
    }
}
