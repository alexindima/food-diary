using FoodDiary.Application.Abstractions.Admin.Models;
using FoodDiary.Application.Abstractions.Common.Models;
using FoodDiary.Presentation.Api.Features.Admin.Responses;
using FoodDiary.Presentation.Api.Responses;

namespace FoodDiary.Presentation.Api.Features.Admin.Mappings;

public static class AdminBillingHttpResponseMappings {
    extension(AdminBillingRevenueSummaryReadModel model) {
        public AdminBillingRevenueSummaryHttpResponse ToHttpResponse() =>
                new(
                    model.FromUtc,
                    model.ToUtc,
                    [.. model.Currencies.Select(static currency => new AdminBillingRevenueCurrencyHttpResponse(
                    currency.Currency,
                    currency.Gross,
                    currency.Refunds,
                    currency.Chargebacks,
                    currency.Reversals,
                    currency.Net,
                    currency.SuccessfulPayments,
                    currency.Tax,
                    currency.PaddleFees,
                    currency.PaddleEarnings,
                    currency.EarningsTrackedPayments))]);
    }

    extension(AdminBillingSubscriptionReadModel model) {
        public AdminBillingSubscriptionHttpResponse ToHttpResponse() {
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
    }

    extension(AdminBillingPaymentReadModel model) {
        public AdminBillingPaymentHttpResponse ToHttpResponse() {
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
                model.ModifiedOnUtc,
                model.Tax,
                model.Fee,
                model.Earnings,
                model.PayoutCurrency,
                model.PayoutEarnings);
        }
    }

    extension(AdminBillingWebhookEventReadModel model) {
        public AdminBillingWebhookEventHttpResponse ToHttpResponse() {
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
                model.ModifiedOnUtc,
                model.ReceivedAtUtc,
                model.AttemptCount,
                model.NextAttemptAtUtc);
        }
    }

    extension(PagedResponse<AdminBillingSubscriptionReadModel> response) {
        public PagedHttpResponse<AdminBillingSubscriptionHttpResponse> ToBillingSubscriptionsHttpResponse(
        ) {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }

    extension(PagedResponse<AdminBillingPaymentReadModel> response) {
        public PagedHttpResponse<AdminBillingPaymentHttpResponse> ToBillingPaymentsHttpResponse(
        ) {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }

    extension(PagedResponse<AdminBillingWebhookEventReadModel> response) {
        public PagedHttpResponse<AdminBillingWebhookEventHttpResponse> ToBillingWebhookEventsHttpResponse(
        ) {
            return response.ToPagedHttpResponse(ToHttpResponse);
        }
    }
}
