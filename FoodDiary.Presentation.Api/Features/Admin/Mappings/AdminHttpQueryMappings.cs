using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminContentReports;
using FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;
using FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;
using FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;
using FoodDiary.Application.Admin.Queries.GetAdminLessons;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;
using FoodDiary.Application.Admin.Queries.GetAdminUsers;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpQueryMappings {
    public static GetAdminEmailTemplatesQuery ToEmailTemplatesQuery() => new();
    public static GetAdminAiPromptsQuery ToAiPromptsQuery() => new();
    public static GetAdminLessonsQuery ToLessonsQuery() => new();

    public static GetAdminUsersQuery ToQuery(this GetAdminUsersHttpQuery query) {
        return new GetAdminUsersQuery(query.Page, query.Limit, query.Search, query.IncludeDeleted);
    }

    public static GetAdminDashboardSummaryQuery ToQuery(this GetAdminDashboardHttpQuery query) {
        return new GetAdminDashboardSummaryQuery(Math.Clamp(query.Recent, 1, 20));
    }

    public static GetAdminAiUsageSummaryQuery ToQuery(this GetAdminAiUsageSummaryHttpQuery query) {
        return new GetAdminAiUsageSummaryQuery(query.From, query.To);
    }

    public static GetAdminContentReportsQuery ToQuery(this GetAdminContentReportsHttpQuery query) {
        return new GetAdminContentReportsQuery(query.Status, query.Page, query.Limit);
    }

    public static GetAdminMailInboxMessagesQuery ToQuery(this GetAdminMailInboxMessagesHttpQuery query) {
        return new GetAdminMailInboxMessagesQuery(query.Limit);
    }

    public static GetAdminMailInboxMessageDetailsQuery ToMailInboxMessageDetailsQuery(this Guid id) {
        return new GetAdminMailInboxMessageDetailsQuery(id);
    }
}
