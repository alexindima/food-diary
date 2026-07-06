using FoodDiary.Application.Abstractions.Export.Common;
using FoodDiary.Application.Abstractions.Meals.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator(
    HttpClient httpClient,
    IDiaryPdfReportTextProvider textProvider,
    TimeProvider timeProvider) : IDiaryPdfGenerator {
    internal DiaryPdfGenerator()
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(5) }, new DefaultDiaryPdfReportTextProvider(), TimeProvider.System) {
    }

    internal DiaryPdfGenerator(HttpClient httpClient)
        : this(httpClient, new DefaultDiaryPdfReportTextProvider(), TimeProvider.System) {
    }

    public async Task<byte[]> GenerateAsync(
        IReadOnlyList<MealConsumptionReadModel> meals,
        DateTime dateFrom,
        DateTime dateTo,
        string? locale,
        int? timeZoneOffsetMinutes,
        string? reportOrigin,
        CancellationToken cancellationToken) {
        QuestPDF.Settings.License = LicenseType.Community;

        DiaryPdfReportTexts texts = textProvider.GetTexts(locale);
        bool useCompactMealsMode = ShouldUseCompactMealsMode(dateFrom, dateTo);
        IReadOnlyDictionary<Guid, byte[]> mealImages = useCompactMealsMode
            ? new Dictionary<Guid, byte[]>()
            : await LoadMealImagesAsync(meals, cancellationToken).ConfigureAwait(false);
        var report = DiaryReportData.Create(
            meals,
            dateFrom,
            dateTo,
            mealImages,
            useCompactMealsMode,
            texts,
            timeZoneOffsetMinutes,
            ResolveReportHost(reportOrigin),
            timeProvider.GetUtcNow().UtcDateTime);

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
