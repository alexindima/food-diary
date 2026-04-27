export type BillingPlan = 'monthly' | 'yearly';
export type BillingProvider = 'Paddle' | 'YooKassa' | 'Stripe' | string;

export interface BillingOverview {
    isPremium: boolean;
    subscriptionStatus: string | null;
    plan: BillingPlan | null;
    currentPeriodEndUtc: string | null;
    cancelAtPeriodEnd: boolean;
    manageBillingAvailable: boolean;
    provider: string;
    paddleClientToken: string | null;
    availableProviders: BillingProvider[];
}

export interface CheckoutSessionResponse {
    sessionId: string;
    url: string;
    plan: BillingPlan;
}

export interface PortalSessionResponse {
    url: string;
}
