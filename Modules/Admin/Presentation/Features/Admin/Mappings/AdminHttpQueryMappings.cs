using FoodDiary.Application.Admin.Queries.GetAdminAiUsageSummary;
using FoodDiary.Application.Admin.Queries.GetAdminBillingPayments;
using FoodDiary.Application.Admin.Queries.GetAdminBillingRevenueSummary;
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
using FoodDiary.Modules.Fasting.Application.Queries.GetFastingTelemetrySummary;
using FoodDiary.Application.Marketing.Queries.GetMarketingAttributionSummary;
using FoodDiary.Presentation.Api.Features.Admin.Requests;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpQueryMappings {
    extension(GetAdminBillingHttpQuery query) {
        public GetAdminBillingRevenueSummaryQuery ToRevenueSummaryQuery() =>
                new(query.FromUtc, query.ToUtc);

        public GetAdminBillingSubscriptionsQuery ToSubscriptionsQuery() {
            return new GetAdminBillingSubscriptionsQuery(
                query.Page,
                query.Limit,
                query.Provider,
                query.Status,
                query.Search,
                query.FromUtc,
                query.ToUtc);
        }

        public GetAdminBillingPaymentsQuery ToPaymentsQuery() {
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

        public GetAdminBillingWebhookEventsQuery ToWebhookEventsQuery() {
            return new GetAdminBillingWebhookEventsQuery(
                query.Page,
                query.Limit,
                query.Provider,
                query.Status,
                query.Search,
                query.FromUtc,
                query.ToUtc);
        }
    }

    public static GetAdminEmailTemplatesQuery ToEmailTemplatesQuery() => new();
    public static GetAdminAiPromptsQuery ToAiPromptsQuery() => new();
    public static GetAdminLessonsQuery ToLessonsQuery() => new();

    extension(GetAdminUsersHttpQuery query) {
        public GetAdminUsersQuery ToQuery() {
            return new GetAdminUsersQuery(query.Page, query.Limit, query.Search, ResolveUserStatus(query));
        }
    }

    extension(Guid id) {
        public GetAdminUserQuery ToAdminUserQuery() {
            return new GetAdminUserQuery(id);
        }

        public GetAdminMailInboxMessageDetailsQuery ToMailInboxMessageDetailsQuery() {
            return new GetAdminMailInboxMessageDetailsQuery(id);
        }
    }

    extension(GetAdminUserRoleAuditHttpQuery query) {
        public GetAdminUserRoleAuditQuery ToRoleAuditQuery(Guid userId) {
            return new GetAdminUserRoleAuditQuery(userId, query.Limit);
        }
    }

    private static UserAccountStatusFilter ResolveUserStatus(GetAdminUsersHttpQuery query) {
        if (Enum.TryParse(query.Status, ignoreCase: true, out UserAccountStatusFilter status)) {
            return status;
        }

        return query.IncludeDeleted ? UserAccountStatusFilter.All : UserAccountStatusFilter.Active;
    }

    extension(GetAdminUserLoginEventsHttpQuery query) {
        public GetAdminUserLoginEventsQuery ToQuery() {
            return new GetAdminUserLoginEventsQuery(query.Page, query.Limit, query.UserId, query.Search);
        }
    }

    extension(GetAdminUserLoginSummaryHttpQuery query) {
        public GetAdminUserLoginSummaryQuery ToQuery() {
            return new GetAdminUserLoginSummaryQuery(query.FromUtc, query.ToUtc);
        }
    }

    extension(GetAdminDashboardHttpQuery query) {
        public GetAdminDashboardSummaryQuery ToQuery() {
            return new GetAdminDashboardSummaryQuery(Math.Clamp(query.Recent, 1, 20));
        }
    }

    extension(GetAdminAiUsageSummaryHttpQuery query) {
        public GetAdminAiUsageSummaryQuery ToQuery() {
            return new GetAdminAiUsageSummaryQuery(query.From, query.To);
        }
    }

    extension(GetAdminContentReportsHttpQuery query) {
        public GetAdminContentReportsQuery ToQuery() {
            return new GetAdminContentReportsQuery(query.Status, query.Page, query.Limit);
        }
    }

    extension(GetAdminMailInboxMessagesHttpQuery query) {
        public GetAdminMailInboxMessagesQuery ToQuery() {
            return new GetAdminMailInboxMessagesQuery(query.Limit);
        }
    }

    extension(GetFastingTelemetrySummaryHttpQuery query) {
        public GetFastingTelemetrySummaryQuery ToQuery() {
            return new GetFastingTelemetrySummaryQuery(query.Hours);
        }
    }

    extension(GetMarketingAttributionSummaryHttpQuery query) {
        public GetMarketingAttributionSummaryQuery ToQuery() {
            return new GetMarketingAttributionSummaryQuery(query.Hours);
        }
    }
}
