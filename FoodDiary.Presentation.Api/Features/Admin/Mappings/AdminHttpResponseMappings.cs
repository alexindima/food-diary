using FoodDiary.Application.Admin.Models;
using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminHttpResponseMappings {
    public static AdminUserHttpResponse ToHttpResponse(this AdminUserModel model) {
        return new AdminUserHttpResponse(
            model.Id,
            model.Email,
            model.Username,
            model.FirstName,
            model.LastName,
            model.Language,
            model.IsActive,
            model.IsEmailConfirmed,
            model.CreatedOnUtc,
            model.DeletedAt,
            model.LastLoginAtUtc,
            model.Roles,
            model.AiInputTokenLimit,
            model.AiOutputTokenLimit
        );
    }

    public static AdminImpersonationStartHttpResponse ToHttpResponse(this AdminImpersonationStartModel model) {
        return new AdminImpersonationStartHttpResponse(
            model.AccessToken,
            model.TargetUserId,
            model.TargetEmail,
            model.ActorUserId,
            model.Reason);
    }

    public static AdminImpersonationSessionHttpResponse ToHttpResponse(this AdminImpersonationSessionReadModel model) {
        return new AdminImpersonationSessionHttpResponse(
            model.Id,
            model.ActorUserId,
            model.ActorEmail,
            model.TargetUserId,
            model.TargetEmail,
            model.Reason,
            model.ActorIpAddress,
            model.ActorUserAgent,
            model.StartedAtUtc);
    }

    public static AdminAiPromptHttpResponse ToAiPromptHttpResponse(this AdminAiPromptModel model) {
        return new AdminAiPromptHttpResponse(
            model.Id,
            model.Key,
            model.Locale,
            model.PromptText,
            model.Version,
            model.IsActive,
            model.CreatedOnUtc,
            model.UpdatedOnUtc
        );
    }

    public static AdminLessonHttpResponse ToLessonHttpResponse(this AdminLessonModel model) {
        return new AdminLessonHttpResponse(
            model.Id,
            model.Title,
            model.Content,
            model.Summary,
            model.Locale,
            model.Category,
            model.Difficulty,
            model.EstimatedReadMinutes,
            model.SortOrder,
            model.CreatedOnUtc,
            model.ModifiedOnUtc
        );
    }

    public static AdminLessonsImportHttpResponse ToLessonsImportHttpResponse(this AdminLessonsImportModel model) {
        return new AdminLessonsImportHttpResponse(
            model.ImportedCount,
            model.Lessons.Select(static item => item.ToLessonHttpResponse()).ToList());
    }

    public static AdminEmailTemplateHttpResponse ToHttpResponse(this AdminEmailTemplateModel model) {
        return new AdminEmailTemplateHttpResponse(
            model.Id,
            model.Key,
            model.Locale,
            model.Subject,
            model.HtmlBody,
            model.TextBody,
            model.IsActive,
            model.CreatedOnUtc,
            model.UpdatedOnUtc
        );
    }

    public static AdminDashboardSummaryHttpResponse ToHttpResponse(this AdminDashboardSummaryModel model) {
        return new AdminDashboardSummaryHttpResponse(
            model.TotalUsers,
            model.ActiveUsers,
            model.PremiumUsers,
            model.DeletedUsers,
            model.PendingReportsCount,
            model.RecentUsers.ToHttpResponseList(ToHttpResponse)
        );
    }

    public static AdminAiUsageSummaryHttpResponse ToHttpResponse(this AdminAiUsageSummaryModel model) {
        return new AdminAiUsageSummaryHttpResponse(
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens,
            model.ByDay.ToHttpResponseList(ToHttpResponse),
            model.ByOperation.ToHttpResponseList(ToHttpResponse),
            model.ByModel.ToHttpResponseList(ToHttpResponse),
            model.ByUser.ToHttpResponseList(ToHttpResponse)
        );
    }

    public static PagedHttpResponse<AdminUserHttpResponse> ToHttpResponse(this PagedResponse<AdminUserModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    public static PagedHttpResponse<AdminImpersonationSessionHttpResponse> ToImpersonationSessionsHttpResponse(
        this PagedResponse<AdminImpersonationSessionReadModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    public static AdminBillingSubscriptionHttpResponse ToHttpResponse(this AdminBillingSubscriptionReadModel model) {
        return new AdminBillingSubscriptionHttpResponse(
            model.Id,
            model.UserId,
            model.UserEmail,
            model.Provider,
            model.ExternalCustomerId,
            model.ExternalSubscriptionId,
            model.ExternalPaymentMethodId,
            model.ExternalPriceId,
            model.Plan,
            model.Status,
            model.CurrentPeriodStartUtc,
            model.CurrentPeriodEndUtc,
            model.CancelAtPeriodEnd,
            model.NextBillingAttemptUtc,
            model.LastWebhookEventId,
            model.LastSyncedAtUtc,
            model.CreatedOnUtc,
            model.ModifiedOnUtc);
    }

    public static AdminBillingPaymentHttpResponse ToHttpResponse(this AdminBillingPaymentReadModel model) {
        return new AdminBillingPaymentHttpResponse(
            model.Id,
            model.UserId,
            model.UserEmail,
            model.BillingSubscriptionId,
            model.Provider,
            model.ExternalPaymentId,
            model.ExternalCustomerId,
            model.ExternalSubscriptionId,
            model.ExternalPaymentMethodId,
            model.ExternalPriceId,
            model.Plan,
            model.Status,
            model.Kind,
            model.Amount,
            model.Currency,
            model.CurrentPeriodStartUtc,
            model.CurrentPeriodEndUtc,
            model.WebhookEventId,
            model.ProviderMetadataJson,
            model.CreatedOnUtc,
            model.ModifiedOnUtc);
    }

    public static AdminBillingWebhookEventHttpResponse ToHttpResponse(this AdminBillingWebhookEventReadModel model) {
        return new AdminBillingWebhookEventHttpResponse(
            model.Id,
            model.Provider,
            model.EventId,
            model.EventType,
            model.ExternalObjectId,
            model.Status,
            model.ProcessedAtUtc,
            model.PayloadJson,
            model.ErrorMessage,
            model.CreatedOnUtc,
            model.ModifiedOnUtc);
    }

    public static PagedHttpResponse<AdminBillingSubscriptionHttpResponse> ToBillingSubscriptionsHttpResponse(
        this PagedResponse<AdminBillingSubscriptionReadModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    public static PagedHttpResponse<AdminBillingPaymentHttpResponse> ToBillingPaymentsHttpResponse(
        this PagedResponse<AdminBillingPaymentReadModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    public static PagedHttpResponse<AdminBillingWebhookEventHttpResponse> ToBillingWebhookEventsHttpResponse(
        this PagedResponse<AdminBillingWebhookEventReadModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    public static AdminContentReportHttpResponse ToHttpResponse(this AdminContentReportModel model) {
        return new AdminContentReportHttpResponse(
            model.Id, model.ReporterId, model.TargetType, model.TargetId,
            model.Reason, model.Status, model.AdminNote,
            model.CreatedAtUtc, model.ReviewedAtUtc);
    }

    public static PagedHttpResponse<AdminContentReportHttpResponse> ToHttpResponse(
        this PagedResponse<AdminContentReportModel> response) {
        return response.ToPagedHttpResponse(ToHttpResponse);
    }

    public static AdminMailInboxMessageSummaryHttpResponse ToHttpResponse(this AdminMailInboxMessageSummaryModel model) {
        return new AdminMailInboxMessageSummaryHttpResponse(
            model.Id,
            model.FromAddress,
            model.ToRecipients,
            model.Subject,
            model.Status,
            model.ReceivedAtUtc);
    }

    public static AdminMailInboxMessageDetailsHttpResponse ToHttpResponse(this AdminMailInboxMessageDetailsModel model) {
        return new AdminMailInboxMessageDetailsHttpResponse(
            model.Id,
            model.MessageId,
            model.FromAddress,
            model.ToRecipients,
            model.Subject,
            model.TextBody,
            model.HtmlBody,
            model.RawMime,
            model.Status,
            model.ReceivedAtUtc);
    }

    private static AdminAiUsageDailyHttpResponse ToHttpResponse(this AdminAiUsageDailyModel model) {
        return new AdminAiUsageDailyHttpResponse(
            model.Date,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens
        );
    }

    private static AdminAiUsageBreakdownHttpResponse ToHttpResponse(this AdminAiUsageBreakdownModel model) {
        return new AdminAiUsageBreakdownHttpResponse(
            model.Key,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens
        );
    }

    private static AdminAiUsageUserHttpResponse ToHttpResponse(this AdminAiUsageUserModel model) {
        return new AdminAiUsageUserHttpResponse(
            model.Id,
            model.Email,
            model.TotalTokens,
            model.InputTokens,
            model.OutputTokens
        );
    }
}
