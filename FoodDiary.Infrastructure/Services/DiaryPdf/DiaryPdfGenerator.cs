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
    private static readonly SemaphoreSlim PdfRenderGate = new(initialCount: 2, maxCount: 2);
    private readonly TimeSpan _remoteImageDownloadTimeout = DefaultRemoteImageDownloadTimeout;
    private readonly TimeSpan _remoteImageReportTimeout = DefaultRemoteImageReportTimeout;

    internal DiaryPdfGenerator()
        : this(new HttpClient { Timeout = TimeSpan.FromSeconds(5) }, new DefaultDiaryPdfReportTextProvider(), TimeProvider.System) {
    }

    internal DiaryPdfGenerator(HttpClient httpClient)
        : this(httpClient, new DefaultDiaryPdfReportTextProvider(), TimeProvider.System) {
    }

    internal DiaryPdfGenerator(
        HttpClient httpClient,
        TimeSpan remoteImageDownloadTimeout,
        TimeSpan remoteImageReportTimeout)
        : this(httpClient) {
        _remoteImageDownloadTimeout = EnsurePositiveTimeout(remoteImageDownloadTimeout, nameof(remoteImageDownloadTimeout));
        _remoteImageReportTimeout = EnsurePositiveTimeout(remoteImageReportTimeout, nameof(remoteImageReportTimeout));
    }

    public async Task<byte[]> GenerateAsync(
        IReadOnlyList<MealProjectionReadModel> meals,
        DateTime dateFrom,
        DateTime dateTo,
        string? locale,
        int? timeZoneOffsetMinutes,
        string? reportOrigin,
        CancellationToken cancellationToken) {
        QuestPDF.Settings.License = LicenseType.Community;
        await PdfRenderGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try {
            return await GenerateCoreAsync(
                meals,
                dateFrom,
                dateTo,
                locale,
                timeZoneOffsetMinutes,
                reportOrigin,
                cancellationToken).ConfigureAwait(false);
        } finally {
            PdfRenderGate.Release();
        }
    }

    private async Task<byte[]> GenerateCoreAsync(
        IReadOnlyList<MealProjectionReadModel> meals,
        DateTime dateFrom,
        DateTime dateTo,
        string? locale,
        int? timeZoneOffsetMinutes,
        string? reportOrigin,
        CancellationToken cancellationToken) {
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

        cancellationToken.ThrowIfCancellationRequested();
        return document.GeneratePdf();
    }

    private static TimeSpan EnsurePositiveTimeout(TimeSpan timeout, string parameterName) =>
        timeout > TimeSpan.Zero
            ? timeout
            : throw new ArgumentOutOfRangeException(parameterName, timeout, "Timeout must be positive.");
}
