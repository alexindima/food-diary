using FoodDiary.Application.Export.Queries.ExportDiary;

namespace FoodDiary.Presentation.Api.Features.Export.Mappings;

public static class ExportHttpMappings {
    public static ExportDiaryQuery ToQuery(Guid userId, DateTime dateFrom, DateTime dateTo) =>
        new(userId, dateFrom, dateTo);
}
