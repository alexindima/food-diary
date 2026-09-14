using FoodDiary.Presentation.Api.Policies;

namespace FoodDiary.Modules.Admin.Presentation.Requests;

public sealed record GetAdminDashboardHttpQuery(
    [OpenApiNumericRange(PresentationQueryLimits.MinimumPageSize, PresentationQueryLimits.MaximumAdminDashboardRecentItems)] int Recent = 5);
