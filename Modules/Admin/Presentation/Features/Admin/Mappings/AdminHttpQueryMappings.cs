using FoodDiary.Modules.Admin.Application.Queries.GetAdminAiUsageSummary;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingPayments;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingRevenueSummary;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingSubscriptions;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminBillingWebhookEvents;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminContentReports;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminDashboardSummary;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminAiPrompts;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminEmailTemplates;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminLessons;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessageDetails;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessagePage;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminMailInboxMessages;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUser;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUserRoleAudit;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUserLoginEvents;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUserLoginSummary;
using FoodDiary.Modules.Admin.Application.Queries.GetAdminUsers;
using FoodDiary.Application.Abstractions.Users.Common;
using FoodDiary.Modules.Fasting.Application.Queries.GetFastingTelemetrySummary;
using FoodDiary.Application.Marketing.Queries.GetMarketingAttributionSummary;
using FoodDiary.Modules.Admin.Presentation.Features.Admin.Requests;

namespace FoodDiary.Modules.Admin.Presentation.Features.Admin.Mappings;

public static class AdminHttpQueryMappings {
    extension(GetAdminMailInboxMessagePageHttpQuery query) {
        public GetAdminMailInboxMessagePageQuery ToQuery() {
            return new GetAdminMailInboxMessagePageQuery(query.Page, query.Limit, query.Recipient, query.Category, query.Unread, query.FromUtc, query.ToUtc, query.Search, query.FromAddress, query.Id);
        }
    }

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
            UserAdministrationFilter? filter = query.From.HasValue || query.To.HasValue || query.Role is not null ||
                query.EmailConfirmed.HasValue || query.LastLoginFrom.HasValue || query.LastLoginTo.HasValue
                ? new UserAdministrationFilter(query.From, query.To, query.Role, query.EmailConfirmed, query.LastLoginFrom, query.LastLoginTo)
                : null;
            return new GetAdminUsersQuery(query.Page, query.Limit, query.Search, ResolveUserStatus(query), filter);
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
            return new GetAdminUserLoginEventsQuery(query.Page, query.Limit, query.UserId, query.Search, query.FromUtc, query.ToUtc, query.Provider, query.Device);
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
            return new GetAdminAiUsageSummaryQuery(query.From, query.To, query.UserId);
        }
    }

    extension(GetAdminContentReportsHttpQuery query) {
        public GetAdminContentReportsQuery ToQuery() {
            return new GetAdminContentReportsQuery(query.Status, query.Page, query.Limit, query.FromUtc, query.ToUtc, query.TargetType, query.ReporterId, query.TargetId);
        }
    }

    extension(GetAdminMailInboxMessagesHttpQuery query) {
        public GetAdminMailInboxMessagesQuery ToQuery() {
            return new GetAdminMailInboxMessagesQuery(query.Limit, query.Recipient, query.Category, query.Unread);
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
