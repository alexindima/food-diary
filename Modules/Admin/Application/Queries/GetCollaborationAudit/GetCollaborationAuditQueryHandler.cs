using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Modules.Admin.Application.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Modules.Admin.Application.Queries.GetCollaborationAudit;

public sealed class GetCollaborationAuditQueryHandler(IAuditEntryReadService readService)
    : IQueryHandler<GetCollaborationAuditQuery, Result<IReadOnlyList<AdminAuditEntryModel>>> {
    public async Task<Result<IReadOnlyList<AdminAuditEntryModel>>> Handle(
        GetCollaborationAuditQuery query,
        CancellationToken cancellationToken) {
        int limit = Math.Clamp(query.Limit, 1, 500);
        IReadOnlyList<AuditEntryReadModel> entries = await readService.GetRecentAsync(
            query.ClientUserId,
            limit,
            cancellationToken).ConfigureAwait(false);
        return Result.Success<IReadOnlyList<AdminAuditEntryModel>>([
            .. entries.Select(entry => new AdminAuditEntryModel(
                entry.Id,
                entry.ActorUserId,
                entry.SubjectClientUserId,
                entry.Action,
                entry.TargetType,
                entry.TargetId,
                entry.Metadata,
                entry.CreatedAtUtc)),
        ]);
    }
}
