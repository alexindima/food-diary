import type { AdminBillingPayment, AdminBillingSubscription, AdminBillingWebhookEvent } from '../api/admin-billing.service';

export interface AdminBillingSubscriptionViewModel extends AdminBillingSubscription {
    currentPeriodStartText: string;
    currentPeriodEndText: string;
    nextBillingAttemptText: string;
    updatedText: string;
    externalCustomerIdText: string;
    externalSubscriptionIdText: string;
    externalPaymentMethodIdText: string;
}

export interface AdminBillingPaymentViewModel extends AdminBillingPayment {
    createdText: string;
    amountText: string;
    externalPaymentIdText: string;
    externalCustomerIdText: string;
    webhookEventIdText: string;
}

export interface AdminBillingWebhookEventViewModel extends AdminBillingWebhookEvent {
    processedText: string;
    eventIdText: string;
    externalObjectIdText: string;
}
