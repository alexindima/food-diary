import type { AdminId, AdminUtcInstant } from '../../../shared/models/semantics/admin-meaning';
export type AdminBillingTab = 'subscriptions' | 'payments' | 'webhook-events';

export type AdminBillingFilters = {
    provider?: string | null;
    status?: string | null;
    kind?: string | null;
    search?: string | null;
    fromUtc?: string | null;
    toUtc?: string | null;
};

export type AdminBillingSubscription = {
    id: AdminId<'billing-subscription'>;
    userId: AdminId<'user'>;
    userEmail: string | null;
    provider: string;
    externalCustomerId: string;
    externalSubscriptionId?: string | null;
    externalPaymentMethodId?: string | null;
    externalPriceId?: string | null;
    plan?: string | null;
    status: string;
    currentPeriodStartUtc?: AdminUtcInstant | null;
    currentPeriodEndUtc?: AdminUtcInstant | null;
    cancelAtPeriodEnd: boolean;
    nextBillingAttemptUtc?: AdminUtcInstant | null;
    lastWebhookEventId?: AdminId<'billing-webhook-event'> | null;
    lastSyncedAtUtc?: AdminUtcInstant | null;
    createdOnUtc: AdminUtcInstant;
    modifiedOnUtc?: AdminUtcInstant | null;
};

export type AdminBillingPayment = {
    id: AdminId<'billing-payment'>;
    userId: AdminId<'user'>;
    userEmail: string | null;
    billingSubscriptionId?: AdminId<'billing-subscription'> | null;
    provider: string;
    externalPaymentId: string;
    externalCustomerId?: string | null;
    externalSubscriptionId?: string | null;
    externalPaymentMethodId?: string | null;
    externalPriceId?: string | null;
    plan?: string | null;
    status: string;
    kind: string;
    amount?: number | null;
    currency?: string | null;
    tax?: number | null;
    fee?: number | null;
    earnings?: number | null;
    payoutCurrency?: string | null;
    payoutEarnings?: number | null;
    currentPeriodStartUtc?: AdminUtcInstant | null;
    currentPeriodEndUtc?: AdminUtcInstant | null;
    webhookEventId?: AdminId<'billing-webhook-event'> | null;
    providerMetadataJson?: string | null;
    createdOnUtc: AdminUtcInstant;
    modifiedOnUtc?: AdminUtcInstant | null;
};

export type AdminBillingWebhookEvent = {
    id: AdminId<'billing-webhook-event'>;
    provider: string;
    eventId: string;
    eventType: string;
    externalObjectId?: string | null;
    status: string;
    processedAtUtc?: AdminUtcInstant | null;
    receivedAtUtc?: AdminUtcInstant | null;
    attemptCount?: number;
    nextAttemptAtUtc?: AdminUtcInstant | null;
    payloadJson?: string | null;
    errorMessage?: string | null;
    createdOnUtc: AdminUtcInstant;
    modifiedOnUtc?: AdminUtcInstant | null;
};

export type AdminBillingRevenueCurrency = {
    currency: string;
    gross: number;
    refunds: number;
    chargebacks: number;
    reversals: number;
    net: number;
    successfulPayments: number;
    tax: number;
    paddleFees: number;
    paddleEarnings: number;
    earningsTrackedPayments: number;
};

export type AdminBillingRevenueSummary = {
    fromUtc: string;
    toUtc: string;
    currencies: AdminBillingRevenueCurrency[];
    renewalPaymentRecords?: number;
    scheduledCancellations?: number;
};

export type PagedResponse<T> = {
    items: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};
