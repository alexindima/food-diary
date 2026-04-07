using FoodDiary.Application.Export.Models;
using FoodDiary.Application.Export.Queries.ExportDiary;

namespace FoodDiary.Presentation.Api.Features.Export.Mappings;

public static class ExportHttpMappings {
    public static ExportDiaryQuery ToQuery(Guid userId, DateTime dateFrom, DateTime dateTo, string format) =>
        new(userId, dateFrom, dateTo, ParseFormat(format));

    private static ExportFormat ParseFormat(string format) =>
        string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase)
            ? ExportFormat.Pdf
            : ExportFormat.Csv;
}
