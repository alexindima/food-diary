using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Queries.ExportCycle;
using FoodDiary.Application.Export.Queries.ExportDiary;

namespace FoodDiary.Presentation.Api.Features.Export.Mappings;

public static class ExportHttpMappings {
    public static ExportDiaryQuery ToQuery(
        Guid userId,
        DateTime dateFrom,
        DateTime dateTo,
        string format,
        string? locale,
        int? timeZoneOffsetMinutes,
        string? reportOrigin) =>
        new(userId, dateFrom, dateTo, ParseFormat(format), locale, timeZoneOffsetMinutes, reportOrigin);

    public static ExportCycleQuery ToCycleQuery(
        Guid userId,
        DateTime dateFrom,
        DateTime dateTo,
        int? timeZoneOffsetMinutes) =>
        new(userId, dateFrom, dateTo, timeZoneOffsetMinutes);

    private static ExportFormat ParseFormat(string format) =>
        string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
            ? ExportFormat.Pdf
            : ExportFormat.Csv;
}
