using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminAudit;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;

public static class AdminAuditJournalHttpMappings {
    public static GetAdminAuditQuery ToQuery(this GetAdminAuditHttpQuery query) => new(new AuditEntryFilter(
        query.Page, query.Limit, query.FromUtc, query.ToUtc, query.ActorUserId, query.SubjectClientUserId,
        query.Action, query.TargetType, query.TargetId));
}
