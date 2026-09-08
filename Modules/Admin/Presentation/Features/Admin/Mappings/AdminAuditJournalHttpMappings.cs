using FoodDiary.Application.Abstractions.Audit.Models;
using FoodDiary.Application.Admin.Queries.GetAdminAudit;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminAuditJournalHttpMappings {
    public static GetAdminAuditQuery ToQuery(this GetAdminAuditHttpQuery query) => new(new AuditEntryFilter(
        query.Page, query.Limit, query.FromUtc, query.ToUtc, query.ActorUserId, query.SubjectClientUserId,
        query.Action, query.TargetType, query.TargetId));
}
