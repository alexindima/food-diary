import type { BillingOverview, BillingPlan, BillingProvider } from '../../../models/billing.models';
import type {
    PremiumOverviewBadgesViewModel,
    PremiumOverviewCardViewModel,
    PremiumOverviewCopyState,
    PremiumPlanCardViewModel,
    PremiumProviderOptionViewModel,
} from './premium-access.types';

export function buildPremiumOverviewCardViewModel(
    overview: BillingOverview | null,
    isPremium: boolean,
    checkoutAvailable: boolean,
    currentPeriodEndLabel: string | null,
): PremiumOverviewCardViewModel {
    const showManageBilling = (overview?.manageBillingAvailable ?? false) && isPremium;

    return {
        copyState: buildPremiumOverviewCopyState(overview, isPremium),
        badges: buildPremiumOverviewBadges(overview, isPremium),
        currentPeriodEndLabel,
        hintKey: getPremiumOverviewHintKey(showManageBilling, checkoutAvailable),
        showManageBilling,
    };
}

export function buildPremiumOverviewBadges(overview: BillingOverview | null, isPremium: boolean): PremiumOverviewBadgesViewModel {
    return {
        planLabelKey: isPremium && overview?.plan !== null && overview?.plan !== undefined ? getPremiumPlanLabelKey(overview.plan) : null,
        statusLabelKey: getPremiumStatusLabelKey(overview?.subscriptionStatus ?? null),
    };
}

export function buildPremiumOverviewCopyState(overview: BillingOverview | null, isPremium: boolean): PremiumOverviewCopyState {
    return {
        stateLabelKey: isPremium ? 'PREMIUM_PAGE.OVERVIEW.PREMIUM_STATE' : 'PREMIUM_PAGE.OVERVIEW.FREE_STATE',
        periodLabelKey: overview?.cancelAtPeriodEnd === true ? 'PREMIUM_PAGE.OVERVIEW.ENDS_ON' : 'PREMIUM_PAGE.OVERVIEW.RENEWS_ON',
        showCancelAtPeriodEndBanner: overview?.cancelAtPeriodEnd ?? false,
    };
}

export function buildPremiumPlanCards(providers: BillingProvider[], loadingPlan: BillingPlan | null): PremiumPlanCardViewModel[] {
    const providerOptions = providers.map(buildPremiumProviderOption);

    return [
        {
            plan: 'monthly',
            titleKey: 'PREMIUM_PAGE.PLANS.MONTHLY.TITLE',
            descriptionKey: 'PREMIUM_PAGE.PLANS.MONTHLY.DESCRIPTION',
            actionKey: 'PREMIUM_PAGE.PLANS.MONTHLY.ACTION',
            isFeatured: false,
            kickerKey: null,
            isLoading: loadingPlan === 'monthly',
            providerOptions,
        },
        {
            plan: 'yearly',
            titleKey: 'PREMIUM_PAGE.PLANS.YEARLY.TITLE',
            descriptionKey: 'PREMIUM_PAGE.PLANS.YEARLY.DESCRIPTION',
            actionKey: 'PREMIUM_PAGE.PLANS.YEARLY.ACTION',
            isFeatured: true,
            kickerKey: 'PREMIUM_PAGE.PLANS.YEARLY.KICKER',
            isLoading: loadingPlan === 'yearly',
            providerOptions,
        },
    ];
}

export function buildPremiumProviderOption(provider: BillingProvider): PremiumProviderOptionViewModel {
    return {
        provider,
        label: getPremiumProviderLabel(provider),
    };
}

export function getPremiumPlanLabelKey(plan: BillingPlan): string {
    return plan === 'yearly' ? 'PREMIUM_PAGE.PLANS.YEARLY.TITLE' : 'PREMIUM_PAGE.PLANS.MONTHLY.TITLE';
}

export function getPremiumStatusLabelKey(status: string | null): string {
    switch (status) {
        case 'active':
            return 'PREMIUM_PAGE.STATUS.ACTIVE';
        case 'trialing':
            return 'PREMIUM_PAGE.STATUS.TRIALING';
        case 'past_due':
            return 'PREMIUM_PAGE.STATUS.PAST_DUE';
        case 'canceled':
            return 'PREMIUM_PAGE.STATUS.CANCELED';
        case 'unpaid':
            return 'PREMIUM_PAGE.STATUS.UNPAID';
        case 'incomplete':
            return 'PREMIUM_PAGE.STATUS.INCOMPLETE';
        case null:
            return 'PREMIUM_PAGE.STATUS.NONE';
        default:
            return 'PREMIUM_PAGE.STATUS.NONE';
    }
}

export function getPremiumProviderLabel(provider: BillingProvider): string {
    switch (provider.toLowerCase()) {
        case 'yookassa':
            return 'YooKassa';
        case 'paddle':
            return 'Paddle';
        case 'stripe':
            return 'Stripe';
        default:
            return provider;
    }
}

function getPremiumOverviewHintKey(showManageBilling: boolean, checkoutAvailable: boolean): string {
    if (showManageBilling) {
        return 'PREMIUM_PAGE.OVERVIEW.MANAGE_HINT';
    }

    return checkoutAvailable ? 'PREMIUM_PAGE.OVERVIEW.CHECKOUT_HINT' : 'PREMIUM_PAGE.OVERVIEW.CHECKOUT_UNAVAILABLE_HINT';
}
