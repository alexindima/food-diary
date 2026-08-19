using FoodDiary.Application.Abstractions.Export.Common;

namespace FoodDiary.Presentation.Api.Features.Export;

public static class ExportRequestLimits {
    public const int MaximumFormatLength = ExportInputLimits.MaximumFormatLength;
    public const int MaximumLocaleLength = ExportInputLimits.MaximumLocaleLength;
    public const int MaximumReportOriginLength = ExportInputLimits.MaximumReportOriginLength;
    public const int MinimumTimeZoneOffsetMinutes = ExportInputLimits.MinimumTimeZoneOffsetMinutes;
    public const int MaximumTimeZoneOffsetMinutes = ExportInputLimits.MaximumTimeZoneOffsetMinutes;
}
