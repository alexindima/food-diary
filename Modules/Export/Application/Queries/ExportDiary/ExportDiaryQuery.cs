using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Modules.Export.Application.Models;

namespace FoodDiary.Modules.Export.Application.Queries.ExportDiary;

public record ExportDiaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    ExportFormat Format = ExportFormat.Csv,
    string? Locale = null,
    int? TimeZoneOffsetMinutes = null,
    string? ReportOrigin = null) : IQuery<Result<FileExportResult>>, IUserRequest;
