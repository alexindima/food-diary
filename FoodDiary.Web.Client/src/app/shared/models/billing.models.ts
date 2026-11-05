export type BillingPlan = 'monthly' | 'yearly';
export type BillingProvider = string;

export type BillingOverview = {
    isPremium: boolean;
    subscriptionStatus: string | null;
    plan: BillingPlan | null;
    subscriptionProvider: string | null;
    currentPeriodStartUtc: string | null;
    currentPeriodEndUtc: string | null;
    nextBillingAttemptUtc: string | null;
    cancelAtPeriodEnd: boolean;
    renewalEnabled: boolean;
    manageBillingAvailable: boolean;
    cancellationAvailable?: boolean;
    premiumTrialStartUtc: string | null;
    premiumTrialEndUtc: string | null;
    premiumTrialActive: boolean;
    premiumTrialUsed: boolean;
    canStartPremiumTrial: boolean;
    provider: string;
    paddleClientToken: string | null;
    paddleMonthlyPriceId?: string | null;
    paddleYearlyPriceId?: string | null;
    availableProviders: BillingProvider[];
    telegramStarsPrice?: number | null;
};

export type CheckoutSessionResponse = {
    sessionId: string;
    url: string;
    plan: BillingPlan;
};

export type PortalSessionResponse = {
    url: string;
};
