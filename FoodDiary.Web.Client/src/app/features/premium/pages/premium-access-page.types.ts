import type { BillingPlan, BillingProvider } from '../models/billing.models';

export interface PremiumOverviewBadgesViewModel {
    planLabelKey: string | null;
    statusLabelKey: string;
}

export interface PremiumOverviewCopyState {
    stateLabelKey: string;
    periodLabelKey: string;
    showCancelAtPeriodEndBanner: boolean;
}

export interface PremiumPlanCardViewModel {
    plan: BillingPlan;
    titleKey: string;
    descriptionKey: string;
    actionKey: string;
    isFeatured: boolean;
    kickerKey: string | null;
    isLoading: boolean;
    providerOptions: PremiumProviderOptionViewModel[];
}

export interface PremiumProviderOptionViewModel {
    provider: BillingProvider;
    label: string;
}

export type PremiumCheckoutRequest = {
    plan: BillingPlan;
    provider?: BillingProvider;
};
