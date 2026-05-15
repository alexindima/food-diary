import type { BillingOverview, BillingProvider } from '../../../premium/models/billing.models';
import type { BillingViewModel } from './user-manage.types';

export function buildBillingView(overview: BillingOverview | null): BillingViewModel | null {
    if (overview === null) {
        return null;
    }

    return {
        overview,
        statusTone: overview.isPremium ? 'success' : 'muted',
        endLabelKey: overview.cancelAtPeriodEnd ? 'USER_MANAGE.BILLING_ACCESS_ENDS' : 'USER_MANAGE.BILLING_PERIOD_END',
        showNextAttempt: !overview.cancelAtPeriodEnd,
        premiumActionVariant: overview.isPremium ? 'secondary' : 'primary',
        premiumActionLabelKey: overview.isPremium ? 'USER_MANAGE.BILLING_VIEW_PREMIUM' : 'USER_MANAGE.BILLING_UPGRADE',
        showManagedSupportNote: overview.isPremium && !overview.manageBillingAvailable,
    };
}

export function getBillingPlanLabelKey(overview: BillingOverview): string {
    if (!overview.isPremium) {
        return 'USER_MANAGE.BILLING_PLAN_FREE';
    }

    return overview.plan === 'yearly' ? 'USER_MANAGE.BILLING_PLAN_PREMIUM_YEARLY' : 'USER_MANAGE.BILLING_PLAN_PREMIUM_MONTHLY';
}

export function getBillingStatusLabelKey(overview: BillingOverview): string {
    switch (overview.subscriptionStatus) {
        case 'active':
            return 'USER_MANAGE.BILLING_STATUS_ACTIVE';
        case 'trialing':
            return 'USER_MANAGE.BILLING_STATUS_TRIALING';
        case 'past_due':
            return 'USER_MANAGE.BILLING_STATUS_PAST_DUE';
        case 'canceled':
            return 'USER_MANAGE.BILLING_STATUS_CANCELED';
        case 'unpaid':
            return 'USER_MANAGE.BILLING_STATUS_UNPAID';
        case 'incomplete':
            return 'USER_MANAGE.BILLING_STATUS_INCOMPLETE';
        case null:
            return 'USER_MANAGE.BILLING_STATUS_FREE';
        default:
            return 'USER_MANAGE.BILLING_STATUS_FREE';
    }
}

export function getBillingProviderLabel(provider: BillingProvider | null | undefined, translate: (key: string) => string): string {
    const normalizedProvider = provider?.trim() ?? '';
    if (normalizedProvider.length === 0) {
        return translate('USER_MANAGE.BILLING_PROVIDER_NONE');
    }

    switch (normalizedProvider.toLowerCase()) {
        case 'yookassa':
            return 'YooKassa';
        case 'paddle':
            return 'Paddle';
        case 'stripe':
            return 'Stripe';
        default:
            return normalizedProvider;
    }
}

export function getBillingRenewalLabelKey(overview: BillingOverview): string {
    if (!overview.isPremium) {
        return 'USER_MANAGE.BILLING_RENEWAL_FREE';
    }

    if (overview.cancelAtPeriodEnd) {
        return 'USER_MANAGE.BILLING_RENEWAL_CANCELING';
    }

    if (overview.renewalEnabled) {
        return 'USER_MANAGE.BILLING_RENEWAL_ENABLED';
    }

    return 'USER_MANAGE.BILLING_RENEWAL_MANUAL';
}
