using FoodDiary.Application.Abstractions.Common.Abstractions.Results;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Export.Models;

namespace FoodDiary.Application.Export.Queries.ExportCycle;

public record ExportCycleQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int? TimeZoneOffsetMinutes = null) : IQuery<Result<FileExportResult>>, IUserRequest;
