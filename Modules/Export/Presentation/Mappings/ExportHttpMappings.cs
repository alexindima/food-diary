using FoodDiary.Modules.Export.Application.Models;
using FoodDiary.Modules.Export.Application.Queries.ExportCycle;
using FoodDiary.Modules.Export.Application.Queries.ExportDiary;

namespace FoodDiary.Modules.Export.Presentation.Mappings;

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
        new(
            userId,
            DateOnly.FromDateTime(dateFrom),
            DateOnly.FromDateTime(dateTo),
            timeZoneOffsetMinutes);

    public static ExportCycleQuery ToSensitiveCycleQuery(
        Guid userId,
        DateTime dateFrom,
        DateTime dateTo,
        string currentPassword,
        int? timeZoneOffsetMinutes) =>
        new(
            userId,
            DateOnly.FromDateTime(dateFrom),
            DateOnly.FromDateTime(dateTo),
            timeZoneOffsetMinutes,
            CycleExportScope.Sensitive,
            currentPassword);

    private static ExportFormat ParseFormat(string format) =>
        string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
            ? ExportFormat.Pdf
            : ExportFormat.Csv;
}
