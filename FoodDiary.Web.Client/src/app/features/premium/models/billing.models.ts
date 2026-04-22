export type BillingPlan = 'monthly' | 'yearly';

export interface BillingOverview {
    isPremium: boolean;
    subscriptionStatus: string | null;
    plan: BillingPlan | null;
    currentPeriodEndUtc: string | null;
    cancelAtPeriodEnd: boolean;
    manageBillingAvailable: boolean;
    provider: string;
    paddleClientToken: string | null;
}

export interface CheckoutSessionResponse {
    sessionId: string;
    url: string;
    plan: BillingPlan;
}

export interface PortalSessionResponse {
    url: string;
}
