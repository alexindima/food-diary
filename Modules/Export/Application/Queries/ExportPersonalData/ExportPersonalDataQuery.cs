using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Export.Application.Models;
using FoodDiary.Results;

namespace FoodDiary.Modules.Export.Application.Queries.ExportPersonalData;

public sealed record ExportPersonalDataQuery(Guid? UserId) : IQuery<Result<FileExportResult>>, IUserRequest;
