import type { AdminBillingPaymentHttpResponse } from '../../../shared/api/sdk/generated/model/admin-billing-payment-http-response';
import type { AdminBillingRevenueCurrencyHttpResponse } from '../../../shared/api/sdk/generated/model/admin-billing-revenue-currency-http-response';
import type { AdminBillingRevenueSummaryHttpResponse } from '../../../shared/api/sdk/generated/model/admin-billing-revenue-summary-http-response';
import type { AdminBillingSubscriptionHttpResponse } from '../../../shared/api/sdk/generated/model/admin-billing-subscription-http-response';
import type { AdminBillingWebhookEventHttpResponse } from '../../../shared/api/sdk/generated/model/admin-billing-webhook-event-http-response';
import { requireSdkFields } from '../../../shared/api/sdk/sdk-response';
import type {
    AdminBillingPayment,
    AdminBillingRevenueCurrency,
    AdminBillingRevenueSummary,
    AdminBillingSubscription,
    AdminBillingWebhookEvent,
} from '../models/admin-billing.models';

export function adminBillingSubscriptionFromSdk(response: AdminBillingSubscriptionHttpResponse): AdminBillingSubscription {
    const value = requireSdkFields(response, [
        'id',
        'userId',
        'provider',
        'externalCustomerId',
        'status',
        'cancelAtPeriodEnd',
        'createdOnUtc',
    ]);
    return { ...value, userEmail: value.userEmail ?? null };
}

export function adminBillingPaymentFromSdk(response: AdminBillingPaymentHttpResponse): AdminBillingPayment {
    const value = requireSdkFields(response, ['id', 'userId', 'provider', 'externalPaymentId', 'status', 'kind', 'createdOnUtc']);
    return { ...value, userEmail: value.userEmail ?? null };
}

export function adminBillingWebhookEventFromSdk(response: AdminBillingWebhookEventHttpResponse): AdminBillingWebhookEvent {
    const value = requireSdkFields(response, ['id', 'provider', 'eventId', 'eventType', 'status', 'createdOnUtc']);
    return { ...value };
}

export function adminBillingRevenueSummaryFromSdk(response: AdminBillingRevenueSummaryHttpResponse): AdminBillingRevenueSummary {
    const value = requireSdkFields(response, ['fromUtc', 'toUtc', 'currencies']);
    return { ...value, currencies: value.currencies.map(item => adminBillingRevenueCurrencyFromSdk(item)) };
}

export function adminBillingRevenueCurrencyFromSdk(response: AdminBillingRevenueCurrencyHttpResponse): AdminBillingRevenueCurrency {
    const value = requireSdkFields(response, [
        'currency',
        'gross',
        'refunds',
        'chargebacks',
        'reversals',
        'net',
        'successfulPayments',
        'tax',
        'paddleFees',
        'paddleEarnings',
        'earningsTrackedPayments',
    ]);
    return { ...value };
}
