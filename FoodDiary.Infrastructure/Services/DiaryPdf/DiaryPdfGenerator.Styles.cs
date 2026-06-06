namespace FoodDiary.Infrastructure.Services.DiaryPdf;

internal sealed partial class DiaryPdfGenerator {
    private const string PageBackground = "#ffffff";
    private const string PanelBackground = "#ffffff";
    private const string CardBackground = "#f8fafc";
    private const string BorderColor = "#cbd5e1";
    private const string GridColor = "#e2e8f0";
    private const string TextColor = "#111827";
    private const string MutedTextColor = "#475569";
    private const string PrimaryColor = "#0f766e";
    private const string PrimaryFillColor = "#d9f8ec";
    private const string ImagePlaceholderBackground = "#f1f5f9";
    private const string MetricBackground = "#ffffff";
    private const string TableHeaderBackground = "#f1f5f9";
    private const string ProteinColor = "#2563eb";
    private const string FatColor = "#b08900";
    private const string CarbColor = "#059669";
    private const string FiberColor = "#6d28d9";
    private const string SatietyColor = "#f59e0b";
    private const string DefaultReportHost = "fooddiary.club";
    private const int MaxMealImageBytes = 2 * 1024 * 1024;
    private const int MealImageThumbnailSize = 320;
    private const int MaxMealImagesPerReport = 60;
    private const int MaxIngredientImagesPerCollage = 4;
    private const int MaxParallelMealImageDownloads = 6;
}
