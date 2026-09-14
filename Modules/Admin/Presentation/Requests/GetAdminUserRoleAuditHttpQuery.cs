using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record GetAdminUserRoleAuditHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumAdminUserRoleAuditEntries)] int Limit = 20);
