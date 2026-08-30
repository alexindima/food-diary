namespace FoodDiary.Application.Abstractions.Export.Common;

public static class ExportInputLimits {
    public const int MaximumFormatLength = 3;
    public const int MaximumLocaleLength = 64;
    public const int MaximumReportOriginLength = 2048;
    public const int MinimumTimeZoneOffsetMinutes = -840;
    public const int MaximumTimeZoneOffsetMinutes = 840;
}
