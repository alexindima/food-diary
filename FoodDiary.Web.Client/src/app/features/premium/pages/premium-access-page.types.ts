import type { BillingPlan, BillingProvider } from '../models/billing.models';

export type PremiumOverviewBadgesViewModel = {
    planLabelKey: string | null;
    statusLabelKey: string;
};

export type PremiumOverviewCopyState = {
    stateLabelKey: string;
    periodLabelKey: string;
    showCancelAtPeriodEndBanner: boolean;
};

export type PremiumPlanCardViewModel = {
    plan: BillingPlan;
    titleKey: string;
    descriptionKey: string;
    actionKey: string;
    isFeatured: boolean;
    kickerKey: string | null;
    isLoading: boolean;
    providerOptions: PremiumProviderOptionViewModel[];
};

export type PremiumProviderOptionViewModel = {
    provider: BillingProvider;
    label: string;
};

export type PremiumCheckoutRequest = {
    plan: BillingPlan;
    provider?: BillingProvider;
};
