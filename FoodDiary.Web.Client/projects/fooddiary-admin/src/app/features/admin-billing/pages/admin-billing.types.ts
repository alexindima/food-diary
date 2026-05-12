import type { AdminBillingPayment, AdminBillingSubscription, AdminBillingWebhookEvent } from '../api/admin-billing.service';

export type AdminBillingSubscriptionViewModel = {
    currentPeriodStartText: string;
    currentPeriodEndText: string;
    nextBillingAttemptText: string;
    updatedText: string;
    externalCustomerIdText: string;
    externalSubscriptionIdText: string;
    externalPaymentMethodIdText: string;
} & AdminBillingSubscription;

export type AdminBillingPaymentViewModel = {
    createdText: string;
    amountText: string;
    externalPaymentIdText: string;
    externalCustomerIdText: string;
    webhookEventIdText: string;
} & AdminBillingPayment;

export type AdminBillingWebhookEventViewModel = {
    processedText: string;
    eventIdText: string;
    externalObjectIdText: string;
} & AdminBillingWebhookEvent;
