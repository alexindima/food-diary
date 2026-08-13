using FoodDiary.Results;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Export.Models;

namespace FoodDiary.Application.Export.Queries.ExportCycle;

public record ExportCycleQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    int? TimeZoneOffsetMinutes = null) : IQuery<Result<FileExportResult>>, IUserRequest;
