using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;
using FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;
using FoodDiary.Application.Admin.Queries.GetAdminUsers;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpQueryMappings {
    public static GetAdminEmailTemplatesQuery ToEmailTemplatesQuery() => new();

    public static GetAdminUsersQuery ToQuery(this GetAdminUsersHttpQuery query) {
        return new GetAdminUsersQuery(query.Page, query.Limit, query.Search, query.IncludeDeleted);
    }

    public static GetAdminDashboardSummaryQuery ToQuery(this GetAdminDashboardHttpQuery query) {
        return new GetAdminDashboardSummaryQuery(Math.Clamp(query.Recent, 1, 20));
    }

    public static GetAdminAiUsageSummaryQuery ToQuery(this GetAdminAiUsageSummaryHttpQuery query) {
        return new GetAdminAiUsageSummaryQuery(query.From, query.To);
    }
}
