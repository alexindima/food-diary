using FoodDiary.Modules.Export.Application.Abstractions.Common;

namespace FoodDiary.Modules.Export.Presentation;

public static class ExportRequestLimits {
    public const int MaximumFormatLength = ExportInputLimits.MaximumFormatLength;
    public const int MaximumLocaleLength = ExportInputLimits.MaximumLocaleLength;
    public const int MaximumReportOriginLength = ExportInputLimits.MaximumReportOriginLength;
    public const int MinimumTimeZoneOffsetMinutes = ExportInputLimits.MinimumTimeZoneOffsetMinutes;
    public const int MaximumTimeZoneOffsetMinutes = ExportInputLimits.MaximumTimeZoneOffsetMinutes;
}
