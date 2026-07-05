using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;
using FoodDiary.Application.Admin.Queries.GetAdminBillingSubscriptions;
using FoodDiary.Application.Admin.Queries.GetAdminBillingWebhookEvents;
using FoodDiary.Application.Admin.Queries.GetAdminContentReports;
using FoodDiary.Application.Admin.Queries.GetAdminDashboardSummary;
using FoodDiary.Application.Admin.Queries.GetAdminAiPrompts;
using FoodDiary.Application.Admin.Queries.GetAdminEmailTemplates;
using FoodDiary.Application.Admin.Queries.GetAdminLessons;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessageDetails;
using FoodDiary.Application.Admin.Queries.GetAdminMailInboxMessages;
using FoodDiary.Application.Admin.Queries.GetAdminUser;
using FoodDiary.Application.Admin.Queries.GetAdminUserRoleAudit;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginEvents;
using FoodDiary.Application.Admin.Queries.GetAdminUserLoginSummary;
using FoodDiary.Application.Admin.Queries.GetAdminUsers;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Application.Fasting.Queries.GetFastingTelemetrySummary;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpQueryMappings {
    public static GetAdminEmailTemplatesQuery ToEmailTemplatesQuery() => new();
    public static GetAdminAiPromptsQuery ToAiPromptsQuery() => new();
    public static GetAdminLessonsQuery ToLessonsQuery() => new();

    public static GetAdminUsersQuery ToQuery(this GetAdminUsersHttpQuery query) {
        return new GetAdminUsersQuery(query.Page, query.Limit, query.Search, ResolveUserStatus(query));
    }

    public static GetAdminUserQuery ToAdminUserQuery(this Guid id) {
        return new GetAdminUserQuery(id);
    }

    public static GetAdminUserRoleAuditQuery ToRoleAuditQuery(this GetAdminUserRoleAuditHttpQuery query, Guid userId) {
        return new GetAdminUserRoleAuditQuery(userId, query.Limit);
    }

    private static UserAccountStatusFilter ResolveUserStatus(GetAdminUsersHttpQuery query) {
        if (Enum.TryParse<UserAccountStatusFilter>(query.Status, ignoreCase: true, out UserAccountStatusFilter status)) {
            return status;
        }

        return query.IncludeDeleted ? UserAccountStatusFilter.All : UserAccountStatusFilter.Active;
    }

    public static GetAdminUserLoginEventsQuery ToQuery(this GetAdminUserLoginEventsHttpQuery query) {
        return new GetAdminUserLoginEventsQuery(query.Page, query.Limit, query.UserId, query.Search);
    }

    public static GetAdminUserLoginSummaryQuery ToQuery(this GetAdminUserLoginSummaryHttpQuery query) {
        return new GetAdminUserLoginSummaryQuery(query.FromUtc, query.ToUtc);
    }

    public static GetAdminDashboardSummaryQuery ToQuery(this GetAdminDashboardHttpQuery query) {
        return new GetAdminDashboardSummaryQuery(Math.Clamp(query.Recent, 1, 20));
    }

    public static GetAdminAiUsageSummaryQuery ToQuery(this GetAdminAiUsageSummaryHttpQuery query) {
        return new GetAdminAiUsageSummaryQuery(query.From, query.To);
    }

    public static GetAdminBillingSubscriptionsQuery ToSubscriptionsQuery(this GetAdminBillingHttpQuery query) {
        return new GetAdminBillingSubscriptionsQuery(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            query.Search,
            query.FromUtc,
            query.ToUtc);
    }

    public static GetAdminBillingPaymentsQuery ToPaymentsQuery(this GetAdminBillingHttpQuery query) {
        return new GetAdminBillingPaymentsQuery(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            query.Kind,
            query.Search,
            query.FromUtc,
            query.ToUtc);
    }

    public static GetAdminBillingWebhookEventsQuery ToWebhookEventsQuery(this GetAdminBillingHttpQuery query) {
        return new GetAdminBillingWebhookEventsQuery(
            query.Page,
            query.Limit,
            query.Provider,
            query.Status,
            query.Search,
            query.FromUtc,
            query.ToUtc);
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

    public static GetFastingTelemetrySummaryQuery ToQuery(this GetFastingTelemetrySummaryHttpQuery query) {
        return new GetFastingTelemetrySummaryQuery(query.Hours);
    }
}
