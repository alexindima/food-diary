using FoodDiary.Application.Contracts.Common.Abstractions.Messaging;
using FoodDiary.Modules.Export.Application.Models;
using FoodDiary.Modules.Users.Contracts.Common;
using FoodDiary.Modules.Users.Domain.Contracts.ValueObjects.Ids;
using FoodDiary.Results;

namespace FoodDiary.Modules.Export.Application.Queries.ExportPersonalData;

public sealed class ExportPersonalDataQueryHandler(ICurrentUserAccessService access, IPersonalDataArchiveReader reader)
    : IQueryHandler<ExportPersonalDataQuery, Result<FileExportResult>> {
    public async Task<Result<FileExportResult>> Handle(ExportPersonalDataQuery query, CancellationToken cancellationToken) {
        Result<UserId> id = await CurrentUserAccessResolver.ResolveAsync(query.UserId, access, cancellationToken);
        if (id.IsFailure) { return Result.Failure<FileExportResult>(id.Error); }
        try {
            byte[] archive = await reader.ReadArchiveAsync(id.Value, cancellationToken);
            return Result.Success(new FileExportResult(archive, "application/zip", "food-diary-personal-data.zip"));
        } catch (PersonalDataArchiveLimitException) {
            return Result.Failure<FileExportResult>(new Error("Privacy.ExportLimit", "Contact support for a complete export of this large account.", ErrorKind.Validation));
        }
    }
}
