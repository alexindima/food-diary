using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminBillingHttpResponseMappings {
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
}
