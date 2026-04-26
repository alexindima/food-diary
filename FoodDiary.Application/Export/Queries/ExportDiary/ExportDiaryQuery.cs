using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Application.Abstractions.Common.Abstractions.Result;
using FoodDiary.Application.Export.Models;

namespace FoodDiary.Application.Export.Queries.ExportDiary;

public record ExportDiaryQuery(
    Guid? UserId,
    DateTime DateFrom,
    DateTime DateTo,
    ExportFormat Format = ExportFormat.Csv) : IQuery<Result<FileExportResult>>, IUserRequest;
