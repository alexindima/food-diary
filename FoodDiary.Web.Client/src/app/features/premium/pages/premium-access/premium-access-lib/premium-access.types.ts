import type { BillingPlan, BillingProvider } from '../../../../../shared/models/billing.models';

export type PremiumOverviewBadgesViewModel = {
    planLabelKey: string | null;
    statusLabelKey: string;
};

export type PremiumOverviewCopyState = {
    stateLabelKey: string;
    periodLabelKey: string;
    showRenewalOffNotice: boolean;
};

export type PremiumOverviewCardViewModel = {
    titleKey: string;
    accessActive: boolean;
    isFreePlan: boolean;
    subscriptionEnded: boolean;
    paymentIssue: boolean;
    statusNeedsAttention: boolean;
    accessPeriodLabelKey: string;
    copyState: PremiumOverviewCopyState;
    badges: PremiumOverviewBadgesViewModel;
    currentPeriodEndLabel: string | null;
    hintKey: string;
    showManageBilling: boolean;
    showStartTrial: boolean;
    details: PremiumSubscriptionDetail[];
};

export type PremiumSubscriptionDetail = {
    labelKey: string;
    value: string;
    translateValue: boolean;
};

export type PremiumSubscriptionDates = {
    periodEnd: string | null;
    periodStart: string | null;
    nextBillingAttempt: string | null;
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
    actionKey: string;
};

export type PremiumCheckoutRequest = {
    plan: BillingPlan;
    provider?: BillingProvider;
};
