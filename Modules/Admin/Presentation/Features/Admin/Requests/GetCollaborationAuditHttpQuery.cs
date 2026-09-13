using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record GetCollaborationAuditHttpQuery(
    Guid? ClientUserId = null,
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumCollaborationAuditEntries)] int Limit = 100);
