using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Common.Abstractions.Messaging;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetCollaborationAudit;

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
