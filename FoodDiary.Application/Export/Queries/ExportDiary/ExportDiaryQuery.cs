using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;
using FoodDiary.Application.Export.Models;

namespace FoodDiary.Application.Export.Queries.ExportDiary;

public record ExportDiaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    ExportFormat Format = ExportFormat.Csv,
    string? Locale = null,
    int? TimeZoneOffsetMinutes = null,
    string? ReportOrigin = null) : IQuery<Result<FileExportResult>>, IUserRequest;
