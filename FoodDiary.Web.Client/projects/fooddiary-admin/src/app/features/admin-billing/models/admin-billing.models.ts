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
    id: string;
    userId: string;
    userEmail: string;
    provider: string;
    externalCustomerId: string;
    externalSubscriptionId?: string | null;
    externalPaymentMethodId?: string | null;
    externalPriceId?: string | null;
    plan?: string | null;
    status: string;
    currentPeriodStartUtc?: string | null;
    currentPeriodEndUtc?: string | null;
    cancelAtPeriodEnd: boolean;
    nextBillingAttemptUtc?: string | null;
    lastWebhookEventId?: string | null;
    lastSyncedAtUtc?: string | null;
    createdOnUtc: string;
    modifiedOnUtc?: string | null;
};

export type AdminBillingPayment = {
    id: string;
    userId: string;
    userEmail: string;
    billingSubscriptionId?: string | null;
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
    currentPeriodStartUtc?: string | null;
    currentPeriodEndUtc?: string | null;
    webhookEventId?: string | null;
    providerMetadataJson?: string | null;
    createdOnUtc: string;
    modifiedOnUtc?: string | null;
};

export type AdminBillingWebhookEvent = {
    id: string;
    provider: string;
    eventId: string;
    eventType: string;
    externalObjectId?: string | null;
    status: string;
    processedAtUtc?: string | null;
    receivedAtUtc?: string | null;
    attemptCount?: number;
    nextAttemptAtUtc?: string | null;
    payloadJson?: string | null;
    errorMessage?: string | null;
    createdOnUtc: string;
    modifiedOnUtc?: string | null;
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
};

export type PagedResponse<T> = {
    items: T[];
    page: number;
    limit: number;
    totalPages: number;
    totalItems: number;
};
