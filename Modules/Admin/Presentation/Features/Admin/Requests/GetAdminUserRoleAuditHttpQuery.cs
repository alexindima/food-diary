using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

public sealed record GetAdminUserRoleAuditHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumAdminUserRoleAuditEntries)] int Limit = 20);
