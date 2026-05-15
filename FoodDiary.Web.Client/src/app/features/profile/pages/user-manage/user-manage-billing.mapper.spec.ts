import { describe, expect, it } from 'vitest';

import type { BillingOverview } from '../../../premium/models/billing.models';
import {
    buildBillingView,
    getBillingPlanLabelKey,
    getBillingProviderLabel,
    getBillingRenewalLabelKey,
    getBillingStatusLabelKey,
} from './user-manage-billing.mapper';

const BILLING_OVERVIEW: BillingOverview = {
    isPremium: true,
    subscriptionStatus: 'active',
    plan: 'monthly',
    subscriptionProvider: 'stripe',
    currentPeriodStartUtc: '2026-01-01T00:00:00Z',
    currentPeriodEndUtc: '2026-02-01T00:00:00Z',
    nextBillingAttemptUtc: '2026-02-01T00:00:00Z',
    cancelAtPeriodEnd: false,
    renewalEnabled: true,
    manageBillingAvailable: true,
    provider: 'stripe',
    paddleClientToken: null,
    availableProviders: ['stripe'],
};

describe('user manage billing mapper', () => {
    it('should build premium billing view', () => {
        expect(buildBillingView(BILLING_OVERVIEW)).toEqual({
            overview: BILLING_OVERVIEW,
            statusTone: 'success',
            endLabelKey: 'USER_MANAGE.BILLING_PERIOD_END',
            showNextAttempt: true,
            premiumActionVariant: 'secondary',
            premiumActionLabelKey: 'USER_MANAGE.BILLING_VIEW_PREMIUM',
            showManagedSupportNote: false,
        });
    });

    it('should build free billing labels', () => {
        const overview: BillingOverview = {
            ...BILLING_OVERVIEW,
            isPremium: false,
            subscriptionStatus: null,
            plan: null,
            subscriptionProvider: null,
            provider: '',
            renewalEnabled: false,
        };

        expect(getBillingPlanLabelKey(overview)).toBe('USER_MANAGE.BILLING_PLAN_FREE');
        expect(getBillingStatusLabelKey(overview)).toBe('USER_MANAGE.BILLING_STATUS_FREE');
        expect(getBillingProviderLabel(overview.subscriptionProvider ?? overview.provider, key => `translated:${key}`)).toBe(
            'translated:USER_MANAGE.BILLING_PROVIDER_NONE',
        );
        expect(getBillingRenewalLabelKey(overview)).toBe('USER_MANAGE.BILLING_RENEWAL_FREE');
    });

    it('should build premium labels', () => {
        const overview: BillingOverview = {
            ...BILLING_OVERVIEW,
            plan: 'yearly',
            subscriptionProvider: 'yookassa',
            cancelAtPeriodEnd: true,
        };

        expect(getBillingPlanLabelKey(overview)).toBe('USER_MANAGE.BILLING_PLAN_PREMIUM_YEARLY');
        expect(getBillingStatusLabelKey(overview)).toBe('USER_MANAGE.BILLING_STATUS_ACTIVE');
        expect(getBillingProviderLabel(overview.subscriptionProvider, key => key)).toBe('YooKassa');
        expect(getBillingRenewalLabelKey(overview)).toBe('USER_MANAGE.BILLING_RENEWAL_CANCELING');
    });
});
