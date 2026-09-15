using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Modules.Export.Application.Models;

namespace FoodDiary.Modules.Export.Application.Queries.ExportCycle;

public record ExportCycleQuery(
    Guid? UserId,
    DateOnly DateFrom,
    DateOnly DateTo,
    int? TimeZoneOffsetMinutes = null,
    CycleExportScope Scope = CycleExportScope.Standard,
    string? CurrentPassword = null) : IQuery<Result<FileExportResult>>, IUserRequest {
    public ExportCycleQuery(
        Guid? userId,
        DateTime dateFrom,
        DateTime dateTo,
        int? timeZoneOffsetMinutes = null)
        : this(
            userId,
            DateOnly.FromDateTime(dateFrom),
            DateOnly.FromDateTime(dateTo),
            timeZoneOffsetMinutes) {
    }
}
