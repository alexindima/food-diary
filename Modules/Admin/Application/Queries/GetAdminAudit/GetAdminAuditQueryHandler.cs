using FoodDiary.Application.Abstractions.Audit.Common;
using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Abstractions.Common.Abstractions.Messaging;
using FoodDiary.Application.Admin.Models;
using FoodDiary.Results;

namespace FoodDiary.Application.Admin.Queries.GetAdminAudit;

public sealed class GetAdminAuditQueryHandler(IAuditEntryJournal journal) : IQueryHandler<GetAdminAuditQuery, Result<AdminAuditPage>> {
    public async Task<Result<AdminAuditPage>> Handle(GetAdminAuditQuery query, CancellationToken cancellationToken) {
        AuditEntryPage page = await journal.GetPageAsync(query.Filter, cancellationToken).ConfigureAwait(false);
        return Result.Success(new AdminAuditPage(page.Items.Select(entry => new AdminAuditEntryModel(
            entry.Id, entry.ActorUserId, entry.SubjectClientUserId, entry.Action, entry.TargetType, entry.TargetId,
            entry.Metadata, entry.CreatedAtUtc)).ToList(), page.TotalItems));
    }
}
